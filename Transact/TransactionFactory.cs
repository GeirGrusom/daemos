﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Transact
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
    }

    public delegate void MutateTransactionDataDelegate(ref TransactionMutableData data);

    public static class TransactionExtensions
    {
        public static Task Lock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.LockTransaction(scope.Id, flags, timeout);
        }

        public static Task<bool> TryLock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.TryLockTransaction(scope.Id, flags, timeout);
        }

        public static Task Free(this Transaction transaction)
        {
            return transaction.Storage.FreeTransaction(transaction.Id);
        }

        public static Task<Transaction> CreateDelta(this Transaction original, MutateTransactionDataDelegate delta)
        {
            var data = original.Data;
            data.Revision += 1;
            data.Created = DateTime.UtcNow;
            data.Expires = data.Created; // Unless otherwise stated transactions expire immediately.
            data.Expired = null;
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

            return original.Storage.CommitTransactionDelta(original, newTransaction);
        }

        public static Task<Transaction> Fetch(this Transaction scope, int revision = -1)
        {
            return scope.Storage.FetchTransaction(scope.Id, revision);
        }

        public static Task<Transaction> CreateChild(this Transaction trans, NewChildTransactionData data)
        {
            var newTransaction = new Transaction(data.Id, 0, DateTime.UtcNow, data.Expires, null, data.Payload, data.Script, TransactionState.Initialized, new TransactionRevision(trans.Id, trans.Revision), trans.Storage);
            return trans.Storage.CreateTransaction(newTransaction);
        }

        public static async Task<Transaction> Authorize(this Transaction scope)
        {
            var last = await Fetch(scope);
            return await CreateDelta(last, (ref TransactionMutableData x)=>
            {
                x.Expires = null;
                x.State = TransactionState.Authorized;
            });
        }

        public static async Task<Transaction> Expire(this Transaction transaction)
        {
            var last = await Fetch(transaction);
            var data = last.Data;
            data.Expired = DateTime.UtcNow;
            data.Expires = null;
            data.Revision = last.Revision + 1;
            data.Script = null;
            return await transaction.Storage.CommitTransactionDelta(last, new Transaction(data, transaction.Storage));
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
        Task LockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);
        Task<bool> TryLockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);
        Task FreeTransaction(Guid id);
        Task<bool> IsTransactionLocked(Guid id);
        Task<bool> TransactionExists(Guid id);
        Task<Transaction> FetchTransaction(Guid id, int revision = -1);
        Task<Transaction> CreateTransaction(Transaction transaction);
        Task<Transaction> CommitTransactionDelta(Transaction original, Transaction next);
        IEnumerable<Transaction> GetExpiringTransactions(DateTime now, CancellationToken cancel);
        IEnumerable<Transaction> GetChildTransactions(Guid transaction, params TransactionState[] state); 

        event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        Task<Transaction> WaitFor(Func<Transaction, bool> predicate, int timeout = Timeout.Infinite);
        Task<IEnumerable<Transaction>> GetChain(Guid id);

        IQueryable<Transaction> Query();
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

        public async Task<Transaction> ContinueTransaction(Guid id, int timeout = Timeout.Infinite)
        {
            var trans = await Storage.FetchTransaction(id);
            if (!await trans.TryLock(timeout: timeout))
            {
                throw new TimeoutException("Could not get a lock on the transaction.");
            }
            return trans;
        }

        public async Task<Transaction> StartTransaction(Guid id)
        {

            var trans = await Storage.CreateTransaction(new Transaction(id, 0, DateTime.UtcNow, null, null, new ExpandoObject(),
                null, TransactionState.Initialized, null, Storage));
            await trans.Lock();
            return trans;
        }

        public async Task<Transaction> StartTransaction()
        {
            var id = Guid.NewGuid();
            var trans = await Storage.CreateTransaction(new Transaction(id, 0, DateTime.UtcNow, null, null, new ExpandoObject(),
                null, TransactionState.Initialized, null, Storage));
            await trans.Lock();
            return trans;
        }
    }
}