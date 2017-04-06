using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Markurion
{
    public abstract class TransactionStorageBase : ITransactionStorage
    {
        public event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        private ITransactionMatchCompiler _transactionMatchCompiler;
        private readonly AutoResetEvent _nextExpiringTransactionChangedEvent;
        private DateTime? _nextExpiringTransaction;
        private readonly ITimeService _timeService;

        public ITimeService TimeService => _timeService;

        protected TransactionStorageBase()
        {
            _nextExpiringTransactionChangedEvent = new AutoResetEvent(true);
            _timeService = new UtcTimeService();
        }

        protected TransactionStorageBase(ITimeService timeService)
        {
            _nextExpiringTransactionChangedEvent = new AutoResetEvent(true);
            _timeService = timeService;
        }

        protected virtual void OnTransactionCommitted(Transaction transaction)
        {
            if(transaction.Expires != null && (_nextExpiringTransaction == null || transaction.Expires < _nextExpiringTransaction))
            {
                _nextExpiringTransaction = transaction.Expires;
                _nextExpiringTransactionChangedEvent.Set();
            }
            TransactionCommitted?.Invoke(this, new TransactionCommittedEventArgs(transaction));
        }

        public abstract Task<Transaction> CommitTransactionDelta(Transaction original, Transaction next);
        public abstract Task<Transaction> CreateTransaction(Transaction transaction);
        public abstract Task<Transaction> FetchTransaction(Guid id, int revision = -1);
        public abstract Task FreeTransaction(Guid id);
        public abstract Task<IEnumerable<Transaction>> GetChain(Guid id);
        public abstract Task<IEnumerable<Transaction>> GetChildTransactions(Guid transaction, params TransactionState[] state);
        public abstract Task<bool> IsTransactionLocked(Guid id);
        public abstract Task LockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = -1);
        public abstract Task<IQueryable<Transaction>> Query();
        public abstract Task<bool> TransactionExists(Guid id);
        public abstract Task<bool> TryLockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = -1);
        public abstract Task Open();
        public virtual async Task<Transaction> WaitFor(Func<Transaction, bool> predicate, int timeout)
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

        public Task<List<Transaction>> GetExpiringTransactions(CancellationToken cancel)
        {
            WaitForExpiringTransactions(cancel);
            return GetExpiringTransactionsInternal(cancel);
        }

        protected abstract Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel);

        protected void SetNextExpiringTransactionTime(DateTime? next)
        {
            _nextExpiringTransaction = next;
        }

        protected virtual void WaitForExpiringTransactions(CancellationToken cancel)
        {
            try
            {
                // This if-block looks like a race condition, but it *shouldn't* be.
                // The idea is that _nextExpiringTransactionChangedEvent will be open
                // or open soon if _nextExpiringTransaction changed in the meantime.
                // No other threads is supposed to open it.
                if (_nextExpiringTransaction == null)
                {
                    _nextExpiringTransactionChangedEvent.WaitOne();
                    return;
                }

                int delta;
                do
                {
                    var deltaSpan = (_nextExpiringTransaction.Value - _timeService.Now());
                    long deltaD = deltaSpan.Ticks / TimeSpan.TicksPerMillisecond;

                    if (deltaD < 0)
                        break;

                    delta = deltaD > int.MaxValue ? int.MaxValue : (int)deltaD;
                    if (delta > 0)
                        _nextExpiringTransactionChangedEvent.WaitOne(delta);
                }
                while (delta > 0);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
