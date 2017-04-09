﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Markurion
{
    public sealed class TransactionCommittedEventArgs : EventArgs
    {
        public Transaction Transaction { get; }

        public TransactionCommittedEventArgs(Transaction transaction)
        {
            Transaction = transaction;
        }
    }

    public sealed class MemoryStorage : TransactionStorageBase
    {
        private class TransactionSlot : IEquatable<TransactionSlot>
        {
            
            public List<Transaction> Chain { get; }
            public Transaction Head { get; internal set; }
            public SemaphoreSlim Lock { get; }

            public TransactionSlot()
            {
                Chain = new List<Transaction>();
                Lock = new SemaphoreSlim(1, 1);
            }

            public TransactionSlot(Transaction head)
                : this()
            {
                Chain.Add(head);
                Head = head;
            }

            public override int GetHashCode()
            {
                if (Chain.Count > 0)
                {
                    return Chain[0].Id.GetHashCode();
                }
                return 0;
            }

            public bool Equals(TransactionSlot other)
            {
                if (Chain.Count == 0)
                    return false;
                if (other.Chain.Count == 0)
                    return false;
                return other.Chain[0].Id == Chain[0].Id;
            }
        }

        private sealed class ReverseDateTimeComparer : IComparer<DateTime>
        {
            public static ReverseDateTimeComparer Instance { get; } = new ReverseDateTimeComparer();
            public int Compare(DateTime x, DateTime y)
            {
                var result = y.CompareTo(x);
                if(result == 0)
                {
                    return 1;
                }
                return result;
            }
        }

        private readonly ConcurrentDictionary<Guid, TransactionSlot> _transactions;
        private readonly SortedList<DateTime, TransactionSlot> _transactionsByExpiriation = new SortedList<DateTime, TransactionSlot>(ReverseDateTimeComparer.Instance);
        private readonly ManualResetEventSlim _expiringEvent;
        public MemoryStorage()
        {
            _transactions = new ConcurrentDictionary<Guid, TransactionSlot>();
            _expiringEvent = new ManualResetEventSlim(false);
        }

        public MemoryStorage(ITimeService timeService)
            : base(timeService)
        {
            _transactions = new ConcurrentDictionary<Guid, TransactionSlot>();
            _expiringEvent = new ManualResetEventSlim(false);
        }


        public override Task Open()
        {
            return Task.CompletedTask;
        }

        public override async Task LockTransaction(Guid id, LockFlags flags, int timeout)
        {
            TransactionSlot slot;
            bool found = _transactions.TryGetValue(id, out slot);
            if (!found)
            {
                if ((flags & LockFlags.CreateIfNotExists) == LockFlags.CreateIfNotExists)
                {
                    lock (_transactionsByExpiriation)
                    {
                        slot = _transactions.GetOrAdd(id, g => new TransactionSlot());
                        _transactionsByExpiriation.Add(DateTime.MaxValue, slot);
                    }
                }
                else
                    throw new InvalidOperationException($"The transaction {id:N} does not exist.");

            }

            if(!await slot.Lock.WaitAsync(timeout))
                throw new TimeoutException();
        }

        public override Task<IEnumerable<Transaction>> GetChildTransactions(Guid id, params TransactionState[] states)
        {
            return Task.FromResult(from slot in _transactions
                let trans = slot.Value.Chain.Last()
                where trans.Parent != null && trans.Parent.Value.Id == id && states.Any(x => x == trans.State)
                select trans);

        }

        public override Task<bool> TryLockTransaction(Guid id, LockFlags flags, int timeout)
        {
            TransactionSlot slot;
            bool found = _transactions.TryGetValue(id, out slot);
            if (!found)
            {
                if ((flags & LockFlags.CreateIfNotExists) == LockFlags.CreateIfNotExists)
                {
                    lock (_transactionsByExpiriation)
                    {
                        slot = _transactions.GetOrAdd(id, g => new TransactionSlot());
                        _transactionsByExpiriation.Add(DateTime.MaxValue, slot);
                    }
                }
                else
                    return Task.FromResult(false);
            }
            return slot.Lock.WaitAsync(timeout);
        }

        public override Task FreeTransaction(Guid id)
        {
            var slot = _transactions[id];
            slot.Lock.Release();
            return Task.FromResult(0);
        }

        public override async Task<bool> IsTransactionLocked(Guid id)
        {
            return !await _transactions[id].Lock.WaitAsync(0);
        }

        public override Task<Transaction> FetchTransaction(Guid id, int revision = -1)
        {
            TransactionSlot slot;
            if (!_transactions.TryGetValue(id, out slot))
            {
                throw new TransactionMissingException(id);
            }
            
            if (revision > -1)
                return Task.FromResult(slot.Chain[revision]);
            return Task.FromResult(slot.Chain[slot.Chain.Count - 1]);
        }

        public override Task<IEnumerable<Transaction>> GetChain(Guid id)
        {
            TransactionSlot slot;
            if (!_transactions.TryGetValue(id, out slot))
            {
                throw new TransactionMissingException(id);
            }

            return Task.FromResult((IEnumerable<Transaction>)slot.Chain);
        }

        public override Task<Transaction> CreateTransaction(Transaction transaction)
        {
            lock (_transactionsByExpiriation)
            {
                var transData = transaction.Data;

                transData.Revision = 1;
                transData.Created = TimeService.Now();
                var insertedTransaction = new Transaction(ref transData, transaction.Storage);

                var slot = new TransactionSlot(insertedTransaction);

                if(!_transactions.TryAdd(insertedTransaction.Id, slot))
                {
                    throw new TransactionExistsException(insertedTransaction.Id);
                }

                int index = _transactionsByExpiriation.IndexOfValue(slot);
                if(index >= 0)
                    _transactionsByExpiriation.RemoveAt(index);
                if(insertedTransaction.Expired == null && insertedTransaction.Expires != null)
                    _transactionsByExpiriation.Add(insertedTransaction.Expires.Value, slot);

                
                OnTransactionCommitted(insertedTransaction);
                return Task.FromResult(insertedTransaction);
            }
        }

        public override Task<Transaction> CommitTransactionDelta(Transaction transaction, Transaction next)
        {
            var slot = _transactions[transaction.Id];
            var last = _transactions[transaction.Id].Chain.Last();

            var trData = next.Data;
            trData.Created = TimeService.Now();
            next = new Transaction(trData, next.Storage);

            if(next.Id != transaction.Id)
                throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));

            if (next.Revision <= 0 || next.Revision > last.Revision + 1 )
                throw new ArgumentException("The specified revision is not a valid revision number.", nameof(next));

            if(next.Revision < last.Revision + 1 && next.Revision > 0)
                throw new TransactionRevisionExistsException(next.Id, next.Revision);

            slot.Chain.Add(next);
            slot.Head = next;

            lock (_transactionsByExpiriation)
            {
                int index = _transactionsByExpiriation.IndexOfValue(slot);
                if(index >= 0)
                    _transactionsByExpiriation.RemoveAt(index);

                if(next.Expired == null && next.Expires != null)
                    _transactionsByExpiriation.Add(next.Expires.Value, slot);
            }
            OnTransactionCommitted(next);
            return Task.FromResult(next);
        }

        public override Task<bool> TransactionExists(Guid id)
        {
            return Task.FromResult(_transactions.ContainsKey(id));
        }

        private static readonly List<Transaction> EmptyTransactionList = new List<Transaction>();
        protected override Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel)
        {
            var now = TimeService.Now();
            DateTime[] list;
            TransactionSlot[] values;
            lock (_transactionsByExpiriation)
            {
                list = _transactionsByExpiriation.Keys.ToArray();
                values = _transactionsByExpiriation.Values.ToArray();
            }

            int min = 0;
            int max = list.Length - 1;

            if (max < 0 || list[max] > now)
            {
                if(values.Length != 0)
                {
                    SetNextExpiringTransactionTime(values.Last().Chain.Last().Expires);
                }
                
                return Task.FromResult(EmptyTransactionList);
            }

            // Binary search
            
            int mid = 0;
            while (min <= max)
            {
                mid = min + (max - min) / 2;

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
                SetNextExpiringTransactionTime(null);
            }
            else
            {
                SetNextExpiringTransactionTime(values.Skip(mid - 1).Select(x => x.Chain.Last().Expires).FirstOrDefault());
            }
            return Task.FromResult(result);

        }

        public override Task<IQueryable<Transaction>> Query()
        {
            return Task.FromResult(_transactions.Select(x => x.Value.Head).AsQueryable());
        }

        public override async Task<Transaction> WaitFor(Func<Transaction, bool> predicate, int timeout)
        {
            SemaphoreSlim sem = new SemaphoreSlim(0, 1);
            Transaction result = null;
            EventHandler<TransactionCommittedEventArgs> ev = null;
            ev = (sender, e) =>
            {
                if (predicate(e.Transaction))
                {
                    result = e.Transaction;
                    TransactionCommitted -= ev;
                    sem.Release();
                }
            };

            TransactionCommitted += ev;

            if (!await sem.WaitAsync(timeout))
                return null;

            return result;
        }
    }
}
