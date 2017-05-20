using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transact
{

    static class DynamicHelper
    {
        public static bool TryGetDynamicProperty<T>(this object value, string name, out T result)
        {
            var dict  = value as IDictionary<string, object>;
            if (dict != null)
            {
                object res;
                if (dict.TryGetValue(name, out res))
                {
                    if (res is T)
                    {
                        result = (T) res;
                        return true;
                    }
                }
                result = default(T);
                return false;
            }
            var type = value.GetType();
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
            {
                if (prop.PropertyType != typeof (T))
                {
                    result = default(T);
                    return false;
                }
                object res = prop.GetValue(value);
                result = (T) res;
                return true;
            }

            result = default(T);
            return false;
        } 
    }

    public sealed class TransactionCommittedEventArgs : EventArgs
    {
        public Transaction Transaction { get; }

        public TransactionCommittedEventArgs(Transaction transaction)
        {
            Transaction = transaction;
        }
    }

    public class MemoryStorage : ITransactionStorage
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
                return y.CompareTo(x);
            }
        }

        public event EventHandler<TransactionCommittedEventArgs> TransactionCommitted; 

        private void OnTransactionCommitted(Transaction transaction)
        {
            var committed = TransactionCommitted;
            if(committed != null)
                committed(this, new TransactionCommittedEventArgs(transaction));
        }
        private readonly AutoResetEvent _nextExpiringTransactionChangedEvent;
        private DateTime? _nextExpiringTransaction;
        private readonly ConcurrentDictionary<Guid, TransactionSlot> _transactions;
        private readonly SortedList<DateTime, TransactionSlot> _transactionsByExpiriation = new SortedList<DateTime, TransactionSlot>(ReverseDateTimeComparer.Instance);
        private readonly ManualResetEventSlim _expiringEvent;
        public MemoryStorage()
        {
            _transactions = new ConcurrentDictionary<Guid, TransactionSlot>();
            _expiringEvent = new ManualResetEventSlim(false);
            _nextExpiringTransactionChangedEvent = new AutoResetEvent(true);
        }
        public async Task LockTransaction(Guid id, LockFlags flags, int timeout)
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

        public IEnumerable<Transaction> GetChildTransactions(Guid id, params TransactionState[] states)
        {
            foreach (var transactionSlot in _transactions)
            {
                var trans = transactionSlot.Value.Chain.Last();
                if (trans.Parent == null)
                    continue;
                if (trans.Parent.Value.Id == id && states.Any(x => x == trans.State))
                    yield return trans;
            }
        }

        public Task<bool> TryLockTransaction(Guid id, LockFlags flags, int timeout)
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

        public Task FreeTransaction(Guid id)
        {
            var slot = _transactions[id];
            slot.Lock.Release();
            return Task.FromResult(0);
        }

        public async Task<bool> IsTransactionLocked(Guid id)
        {
            return !await _transactions[id].Lock.WaitAsync(0);
        }

        public Task<Transaction> FetchTransaction(Guid id, int revision = -1)
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

        public Task<IEnumerable<Transaction>> GetChain(Guid id)
        {
            TransactionSlot slot;
            if (!_transactions.TryGetValue(id, out slot))
            {
                throw new TransactionMissingException(id);
            }

            return Task.FromResult((IEnumerable<Transaction>)slot.Chain);
        }

        public Task<Transaction> CreateTransaction(Transaction transaction)
        {
            lock (_transactionsByExpiriation)
            {
                var slot = _transactions.GetOrAdd(transaction.Id, g =>
                {
                    var newSlot = new TransactionSlot();
                    newSlot.Chain.Add(transaction);
                    newSlot.Head = transaction;
                    return newSlot;
                });
                if (slot.Chain.Count == 0)
                {
                    slot.Chain.Add(transaction);
                    slot.Head = transaction;
                }
                else
                {
                    if(slot.Chain.Count > 1)
                        throw new InvalidOperationException();
                    if (slot.Chain[0] != transaction)
                        throw new InvalidOperationException("That transaction already exists.");
                }

                int index = _transactionsByExpiriation.IndexOfValue(slot);
                if(index >= 0)
                    _transactionsByExpiriation.RemoveAt(index);
                if(transaction.Expired == null && transaction.Expires != null)
                    _transactionsByExpiriation.Add(transaction.Expires.Value, slot);

                if (_transactionsByExpiriation.Count != 0)
                {
                    _expiringEvent.Set();
                    _nextExpiringTransaction = _transactionsByExpiriation.Keys[_transactionsByExpiriation.Keys.Count - 1];
                    _nextExpiringTransactionChangedEvent.Set();
                }
                else
                {
                    _expiringEvent.Reset();
                    _nextExpiringTransaction = null;
                    _nextExpiringTransactionChangedEvent.Set();
                }
                OnTransactionCommitted(transaction);
                return Task.FromResult(transaction);
            }
        }

        public Task<Transaction> CommitTransactionDelta(Transaction transaction, Transaction newTransaction)
        {
            var slot = _transactions[transaction.Id];
            var last = _transactions[transaction.Id].Chain.Last();

            if(newTransaction.Id != transaction.Id)
                throw new ArgumentException("The new transaction has a different id from the chain.", nameof(newTransaction));

            if(newTransaction.Revision != last.Revision + 1)
                throw new ArgumentException("The specified revision already exists.", nameof(newTransaction));

            slot.Chain.Add(newTransaction);
            slot.Head = newTransaction;

            lock (_transactionsByExpiriation)
            {
                int index = _transactionsByExpiriation.IndexOfValue(slot);
                if(index >= 0)
                    _transactionsByExpiriation.RemoveAt(index);

                if(newTransaction.Expired == null && newTransaction.Expires != null)
                    _transactionsByExpiriation.Add(newTransaction.Expires.Value, slot);

                if (_transactionsByExpiriation.Count != 0)
                {
                    _expiringEvent.Set();
                    _nextExpiringTransaction = _transactionsByExpiriation.Keys[_transactionsByExpiriation.Keys.Count - 1];
                    _nextExpiringTransactionChangedEvent.Set();
                }
                else
                {
                    _nextExpiringTransaction = null;
                    _nextExpiringTransactionChangedEvent.Set();
                    _expiringEvent.Reset();
                }
                    
            }
            OnTransactionCommitted(newTransaction);
            return Task.FromResult(newTransaction);
        }

        public Task<bool> TransactionExists(Guid id)
        {
            return Task.FromResult(_transactions.ContainsKey(id));
        }

        public IEnumerable<Transaction> GetExpiringTransactions(DateTime now, CancellationToken cancel)
        {
            // Wait until 
            try
            {
                _expiringEvent.Wait(cancel); // Wait until there are expiring transactions available.
                if (_nextExpiringTransaction != null)
                {
                    var deltaD = (_nextExpiringTransaction.Value - now).TotalMilliseconds;
                    var delta = deltaD > int.MaxValue ? int.MaxValue : (int) deltaD;
                    if (delta > 0)
                        _nextExpiringTransactionChangedEvent.WaitOne(delta);
                }
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<Transaction>();
            }

            
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
                return Enumerable.Empty<Transaction>();

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

            return values.Skip(mid).Select(x => x.Chain.Last());
        }

        public IQueryable<Transaction> Query()
        {
            return _transactions.Select(x => x.Value.Head).AsQueryable();
        }

        public async Task<Transaction> WaitFor(Func<Transaction, bool> predicate, int timeout)
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
