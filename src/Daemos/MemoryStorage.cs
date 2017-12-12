// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class represents an in-memory transaction storage.
    /// </summary>
    public sealed class MemoryStorage : TransactionStorageBase
    {
        private readonly ConcurrentDictionary<Guid, TransactionSlot> transactions;
        private readonly ConcurrentDictionary<TransactionRevision, byte[]> transactionStates;
        private readonly SortedList<DateTime, TransactionSlot> transactionsByExpiriation = new SortedList<DateTime, TransactionSlot>(ReverseDateTimeComparer.Instance);
        private readonly ManualResetEventSlim expiringEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStorage"/> class.
        /// </summary>
        public MemoryStorage()
        {
            this.transactionStates = new ConcurrentDictionary<TransactionRevision, byte[]>();
            this.transactions = new ConcurrentDictionary<Guid, TransactionSlot>();
            this.expiringEvent = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStorage"/> class using the specified <see cref="ITimeService"/>.
        /// </summary>
        /// <param name="timeService">The time service used to get the current time</param>
        public MemoryStorage(ITimeService timeService)
            : base(timeService)
        {
            this.transactionStates = new ConcurrentDictionary<TransactionRevision, byte[]>();
            this.transactions = new ConcurrentDictionary<Guid, TransactionSlot>();
            this.expiringEvent = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Initializes this MemoryStorage.
        /// </summary>
        /// <returns>Completed task</returns>
        public override Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens this MemoryStorage
        /// </summary>
        /// <returns>Completed task</returns>
        public override Task OpenAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task LockTransactionAsync(Guid id, LockFlags flags, int timeout)
        {
            bool found = this.transactions.TryGetValue(id, out TransactionSlot slot);
            if (!found)
            {
                if ((flags & LockFlags.CreateIfNotExists) == LockFlags.CreateIfNotExists)
                {
                    lock (this.transactionsByExpiriation)
                    {
                        slot = this.transactions.GetOrAdd(id, g => new TransactionSlot());
                        this.transactionsByExpiriation.Add(DateTime.MaxValue, slot);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"The transaction {id:N} does not exist.");
                }
            }

            if (!await slot.Lock.WaitAsync(timeout))
            {
                throw new TimeoutException();
            }
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid id, params TransactionState[] states)
        {
            return Task.FromResult(from slot in this.transactions
                                   let trans = slot.Value.Chain.Last()
                                   where trans.Parent != null && trans.Parent.Value.Id == id && states.Any(x => x == trans.State)
                                   select trans);
        }

        /// <inheritdoc/>
        public override Task<byte[]> GetTransactionStateAsync(Guid id, int revision)
        {
            if (this.transactionStates.TryGetValue(new TransactionRevision(id, revision), out byte[] result))
            {
                return Task.FromResult(result.ToArray());
            }

            return Task.FromResult(new byte[0]);
        }

        /// <inheritdoc/>
        public override Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags, int timeout)
        {
            bool found = this.transactions.TryGetValue(id, out TransactionSlot slot);
            if (!found)
            {
                if ((flags & LockFlags.CreateIfNotExists) == LockFlags.CreateIfNotExists)
                {
                    lock (this.transactionsByExpiriation)
                    {
                        slot = this.transactions.GetOrAdd(id, g => new TransactionSlot());
                        this.transactionsByExpiriation.Add(DateTime.MaxValue, slot);
                    }
                }
                else
                {
                    return Task.FromResult(false);
                }
            }

            return slot.Lock.WaitAsync(timeout);
        }

        /// <inheritdoc/>
        public override Task FreeTransactionAsync(Guid id)
        {
            var slot = this.transactions[id];
            slot.Lock.Release();
            return Task.FromResult(0);
        }

        /// <inheritdoc/>
        public override async Task<bool> IsTransactionLockedAsync(Guid id)
        {
            return !await this.transactions[id].Lock.WaitAsync(0);
        }

        /// <inheritdoc/>
        public override Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1)
        {
            if (!this.transactions.TryGetValue(id, out TransactionSlot slot))
            {
                throw new TransactionMissingException(id);
            }

            if (revision > -1)
            {
                return Task.FromResult(slot.Chain[revision]);
            }

            return Task.FromResult(slot.Chain[slot.Chain.Count - 1]);
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<Transaction>> GetChainAsync(Guid id)
        {
            if (!this.transactions.TryGetValue(id, out TransactionSlot slot))
            {
                throw new TransactionMissingException(id);
            }

            return Task.FromResult((IEnumerable<Transaction>)slot.Chain);
        }

        /// <inheritdoc/>
        public override Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            lock (this.transactionsByExpiriation)
            {
                var transData = transaction.Data;

                transData.Revision = 1;
                transData.Created = this.TimeService.Now();
                transData.Payload = ToDictionary(transData.Payload);
                var insertedTransaction = new Transaction(ref transData, transaction.Storage);

                var slot = new TransactionSlot(insertedTransaction);

                if (!this.transactions.TryAdd(insertedTransaction.Id, slot))
                {
                    throw new TransactionExistsException(insertedTransaction.Id);
                }

                int index = this.transactionsByExpiriation.IndexOfValue(slot);
                if (index >= 0)
                {
                    this.transactionsByExpiriation.RemoveAt(index);
                }

                if (insertedTransaction.Expired == null && insertedTransaction.Expires != null)
                {
                    this.transactionsByExpiriation.Add(insertedTransaction.Expires.Value, slot);
                }

                this.OnTransactionCommitted(insertedTransaction);
                return Task.FromResult(insertedTransaction);
            }
        }

        /// <inheritdoc/>
        public override Task<Transaction> CommitTransactionDeltaAsync(Transaction transaction, Transaction next)
        {
            return Task.FromResult(this.CommitTransactionDelta(transaction, next));
        }

        /// <inheritdoc/>
        public override Transaction CommitTransactionDelta(Transaction transaction, Transaction next)
        {
            var slot = this.transactions[transaction.Id];
            var last = this.transactions[transaction.Id].Chain.Last();

            var trData = next.Data;
            trData.Created = this.TimeService.Now();
            trData.Payload = ToDictionary(trData.Payload);
            next = new Transaction(trData, next.Storage);

            if (next.Id != transaction.Id)
            {
                throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));
            }

            if (next.Revision <= 0 || next.Revision > last.Revision + 1)
            {
                throw new ArgumentException("The specified revision is not a valid revision number.", nameof(next));
            }

            if (next.Revision < last.Revision + 1 && next.Revision > 0)
            {
                throw new TransactionRevisionExistsException(next.Id, next.Revision);
            }

            slot.Chain.Add(next);
            slot.Head = next;

            lock (this.transactionsByExpiriation)
            {
                int index = this.transactionsByExpiriation.IndexOfValue(slot);
                if (index >= 0)
                {
                    this.transactionsByExpiriation.RemoveAt(index);
                }

                if (next.Expired == null && next.Expires != null)
                {
                    this.transactionsByExpiriation.Add(next.Expires.Value, slot);
                }
            }

            this.OnTransactionCommitted(next);
            return next;
        }

        /// <inheritdoc/>
        public override Task<bool> TransactionExistsAsync(Guid id)
        {
            return Task.FromResult(this.transactions.ContainsKey(id));
        }

        private static readonly List<Transaction> EmptyTransactionList = new List<Transaction>(0);

        /// <inheritdoc/>
        protected override Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel)
        {
            var now = this.TimeService.Now();
            DateTime[] list;
            TransactionSlot[] values;
            lock (this.transactionsByExpiriation)
            {
                list = this.transactionsByExpiriation.Keys.ToArray();
                values = this.transactionsByExpiriation.Values.ToArray();
            }

            int min = 0;
            int max = list.Length - 1;

            if (max < 0 || list[max] > now)
            {
                if (values.Length != 0)
                {
                    this.SetNextExpiringTransactionTime(values.Last().Chain.Last().Expires);
                }

                return Task.FromResult(EmptyTransactionList);
            }

            // Binary search
            int mid = 0;
            while (min <= max)
            {
                mid = min + ((max - min) / 2);

                var current = list[mid];
                if (current > now)
                {
                    min = mid + 1;
                }
                else if (current < now)
                {
                    max = mid - 1;
                }
                else
                {
                    break;
                }
            }

            var result = values.Skip(mid).Select(x => x.Chain.Last()).ToList();
            if (mid == 0)
            {
                this.SetNextExpiringTransactionTime(null);
            }
            else
            {
                this.SetNextExpiringTransactionTime(values.Skip(mid - 1).Select(x => x.Chain.Last().Expires).FirstOrDefault());
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public override Task<IQueryable<Transaction>> QueryAsync()
        {
            return Task.FromResult(this.transactions.Select(x => x.Value.Head).AsQueryable());
        }

        /// <inheritdoc/>
        public override async Task<Transaction> WaitForAsync(Func<Transaction, bool> predicate, int timeout)
        {
            SemaphoreSlim sem = new SemaphoreSlim(0, 1);
            Transaction result = null;
            EventHandler<TransactionCommittedEventArgs> ev = null;
            ev = (sender, e) =>
            {
                if (predicate(e.Transaction))
                {
                    result = e.Transaction;
                    this.TransactionCommitted -= ev;
                    sem.Release();
                }
            };

            this.TransactionCommitted += ev;

            if (!await sem.WaitAsync(timeout))
            {
                return null;
            }

            return result;
        }

        /// <inheritdoc/>
        public override void SaveTransactionState(Guid id, int revision, byte[] state)
        {
            this.transactionStates[new TransactionRevision(id, revision)] = state;
        }

        /// <inheritdoc/>
        public override Task SaveTransactionStateAsync(Guid id, int revision, byte[] state)
        {
            this.transactionStates[new TransactionRevision(id, revision)] = state;
            return Task.CompletedTask;
        }

        private static IDictionary<string, object> ToDictionary<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is IDictionary<string, object> dict)
            {
                return dict;
            }

            var result = (IDictionary<string, object>)new ExpandoObject();
            var properties = value.GetType().GetProperties();

            foreach (var property in properties)
            {
                object propertyValue = property.GetValue(value);
                if (property.PropertyType.IsClass)
                {
                    if (propertyValue is IDictionary<string, object>)
                    {
                        result[property.Name] = propertyValue;
                    }
                    else
                    {
                        result[property.Name] = ToDictionary(propertyValue);
                    }

                    continue;
                }

                result[property.Name] = propertyValue;
            }

            return result;
        }

        private sealed class ReverseDateTimeComparer : IComparer<DateTime>
        {
            public static ReverseDateTimeComparer Instance { get; } = new ReverseDateTimeComparer();

            public int Compare(DateTime x, DateTime y)
            {
                var result = y.CompareTo(x);
                if (result == 0)
                {
                    return 1;
                }

                return result;
            }
        }

        private sealed class TransactionSlot : IEquatable<TransactionSlot>
        {
            public TransactionSlot()
            {
                this.Chain = new List<Transaction>();
                this.Lock = new SemaphoreSlim(1, 1);
            }

            public TransactionSlot(Transaction head)
                : this()
            {
                this.Chain.Add(head);
                this.Head = head;
            }

            public List<Transaction> Chain { get; }

            public Transaction Head { get; internal set; }

            public SemaphoreSlim Lock { get; }

            public override int GetHashCode()
            {
                if (this.Chain.Count > 0)
                {
                    return this.Chain[0].Id.GetHashCode();
                }

                return 0;
            }

            public bool Equals(TransactionSlot other)
            {
                if (this.Chain.Count == 0)
                {
                    return false;
                }

                if (other.Chain.Count == 0)
                {
                    return false;
                }

                return other.Chain[0].Id == this.Chain[0].Id;
            }
        }
    }
}
