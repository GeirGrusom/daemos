using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Markurion
{
    public struct Handler : IEquatable<Handler>, IEquatable<string>
    {
        private readonly string _value;

        private static readonly Regex InputValidator = new Regex(@"^[a-zA-Z_\-][a-zA-Z0-9_\-]*$", RegexOptions.Compiled);
        private static readonly Regex SingleInputValidator = new Regex(@"^[a-zA-Z_\-][a-zA-Z0-9_\-]*\.[a-zA-Z_\-][a-zA-Z0-9_\-]$", RegexOptions.Compiled);

        public static Handler? TryParse(string input)
        {
            if (!SingleInputValidator.IsMatch(input))
                return null;
            return new Handler(input);
        }

        public static Handler Parse(string input)
        {
            if (!SingleInputValidator.IsMatch(input))
                throw new FormatException();
            return new Handler(input);
        }

        private Handler(string value)
        {
            _value = value;
        }

        public Handler(string owner, string name)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (owner == "")
                throw new ArgumentException("Owner cannot be empty.", nameof(owner));
            if (!InputValidator.IsMatch(owner))
                throw new ArgumentException("Owner was not in a correct format.", nameof(owner));

            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name == "")
                throw new ArgumentException("Name cannot be empty.");
            if (!InputValidator.IsMatch(name))
                throw new ArgumentException("Name was not in a correct format.", nameof(name));

            _value = $"{owner}.{name}";
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is string)
            {
                return Equals((string)obj);
            }
            if (obj is Handler)
            {
                return Equals((Handler)obj);
            }
            return false;
        }

        public static bool operator ==(Handler lhs, Handler rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Handler lhs, Handler rhs)
        {
            return !lhs.Equals(rhs);
        }

        public bool Equals(Handler other)
        {
            return _value.Equals(other._value, StringComparison.Ordinal);
        }

        public bool Equals(string other)
        {
            return _value.Equals(other, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return _value;
        }
    }

    public struct NewChildTransactionData
    {
        public Guid Id { get; set; }
        public DateTime? Expires { get; set; }
        public object Payload { get; set; }
        public string Script { get; set; }
        public object Error { get; set; }
    }

    public delegate void MutateTransactionDataDelegate(ref TransactionMutableData data);

    public static class TransactionExtensions
    {
        public static Task Lock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.LockTransactionAsync(scope.Id, flags, timeout);
        }

        public static Task<bool> TryLock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.TryLockTransactionAsync(scope.Id, flags, timeout);
        }

        public static Task Free(this Transaction transaction)
        {
            return transaction.Storage.FreeTransactionAsync(transaction.Id);
        }

        public static Task<Transaction> CreateDelta(this Transaction original, int nextRevision, bool expired, MutateTransactionDataDelegate delta)
        {
            var data = original.Data;
            data.Revision = nextRevision;
            data.Created = DateTime.UtcNow;
            data.Expired = expired ? data.Created : (DateTime?)null;
            var mutableData = new TransactionMutableData
            {
                Script = data.Script,
                Expires = data.Expires,
                Payload = data.Payload,
                State = data.State
            };
            delta(ref mutableData);

            data.Expires = mutableData.Expires;
            data.Script = mutableData.Script;
            data.Payload = mutableData.Payload;
            data.State = mutableData.State;

            var newTransaction = new Transaction(data, original.Storage);

            return original.Storage.CommitTransactionDeltaAsync(original, newTransaction);
        }

        public static Task<Transaction> Fetch(this Transaction scope, int revision = -1)
        {
            return scope.Storage.FetchTransactionAsync(scope.Id, revision);
        }
    }


    [Flags]
    public enum LockFlags
    {
        None = 0,
        CreateIfNotExists = 1
    }


    public interface ITransactionStorage
    {
        Task LockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);
        Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);
        Task FreeTransactionAsync(Guid id);
        Task<bool> IsTransactionLockedAsync(Guid id);
        Task<bool> TransactionExistsAsync(Guid id);
        Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1);
        Task<Transaction> CreateTransactionAsync([NotNull] Transaction transaction);
        Task<Transaction> CommitTransactionDeltaAsync([NotNull] Transaction original, [NotNull] Transaction next);
        Transaction CommitTransactionDelta([NotNull] Transaction original, [NotNull] Transaction next);
        Task<List<Transaction>> GetExpiringTransactionsAsync(CancellationToken cancel);
        Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionState[] state);
        Task OpenAsync();

        event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        Task<byte[]> GetTransactionStateAsync(Guid id, int revision);

        Task<Transaction> WaitForAsync(Func<Transaction, bool> predicate, int timeout = Timeout.Infinite);
        Task<IEnumerable<Transaction>> GetChainAsync(Guid id);

        Task<IQueryable<Transaction>> QueryAsync();

        Task InitializeAsync();

        void SaveTransactionState(Guid id, int revision, byte[] state);

        ITimeService  TimeService { get; }
    }


    public static class TransactionStorageExtensions
    {

    }

    public sealed class EmptyPayload
    {
        private EmptyPayload()
        {
        }
        public static EmptyPayload Instance { get; } = new EmptyPayload();
    }

    public sealed class TransactionFactory
    {

        public ITransactionStorage Storage { get; }

        public TransactionFactory(ITransactionStorage storage)
        {
            Storage = storage;
        }

        public async Task<Transaction> ContinueTransaction(Guid id, int nextRevision, int timeout = Timeout.Infinite)
        {
            var trans = await Storage.FetchTransactionAsync(id);
            if (!await trans.TryLock(timeout: timeout))
            {
                throw new TimeoutException("Could not get a lock on the transaction.");
            }

            if(nextRevision != trans.Revision + 1)
            {
                await trans.Free();
                throw new TransactionConflictException(id, nextRevision);
            }

            return trans;
        }

        public async Task<Transaction> ContinueTransaction(Guid id, int timeout = Timeout.Infinite)
        {
            var trans = await Storage.FetchTransactionAsync(id);
            if (!await trans.TryLock(timeout: timeout))
            {
                throw new TimeoutException("Could not get a lock on the transaction.");
            }

            return trans;
        }

        public async Task<Transaction> StartTransaction(Guid? id, DateTime? expires, object payload, string script, TransactionRevision? parent)
        {
            var trans = await Storage.CreateTransactionAsync(new Transaction(id ?? Guid.NewGuid(), 0, DateTime.UtcNow, expires, null, payload ?? new ExpandoObject(), script, TransactionState.Initialized, parent, null, Storage));
            await trans.Lock();
            return trans;
        }

        public async Task<Transaction> CreateTransaction(Guid? id, DateTime? expires, object payload, string script, TransactionRevision? parent)
        {
            return await Storage.CreateTransactionAsync(new Transaction(id ?? Guid.NewGuid(), 0, DateTime.UtcNow, expires, null, payload ?? new ExpandoObject(), script, TransactionState.Initialized, parent, null, Storage));
        }

    }
}
