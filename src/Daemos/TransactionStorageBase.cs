// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class is a helper base class for <see cref="ITransactionStorage"/> implementors.
    /// </summary>
    public abstract class TransactionStorageBase : ITransactionStorage
    {
        private readonly Threading.AutoResetEvent nextExpiringTransactionChangedEvent;
        private readonly ITimeService timeService;

        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> rowLock;
        private readonly ReaderWriterLockSlim readerWriterLock;

        private readonly List<Transaction> empty = new List<Transaction>(0);

        private DateTime? nextExpiringTransaction;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionStorageBase"/> class using <see cref="UtcTimeService"/> as the time service.
        /// </summary>
        protected TransactionStorageBase()
            : this(new UtcTimeService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionStorageBase"/> class using the specified <see cref="ITimeService"/>.
        /// </summary>
        /// <param name="timeService">TimeService to use</param>
        protected TransactionStorageBase(ITimeService timeService)
        {
            this.rowLock = new ConcurrentDictionary<Guid, SemaphoreSlim>();
            this.readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            this.nextExpiringTransactionChangedEvent = new Threading.AutoResetEvent(true);
            this.timeService = timeService;
        }

        /// <summary>
        /// This event is triggered when a transaction or a transaction revision is comitted.
        /// </summary>
        public event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        /// <summary>
        /// Gets the <see cref="ITimeService"/> used by this <see cref="TransactionStorageBase"/>.
        /// </summary>
        public ITimeService TimeService => this.timeService;

        /// <summary>
        /// Asynchronously initializes the <see cref="TransactionStorageBase"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task InitializeAsync();

        /// <summary>
        /// Commites the transaction asynchronously.
        /// </summary>
        /// <param name="original">The original for which the next is a revision of</param>
        /// <param name="next">The next revision of original</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the committed transaction.</returns>
        public abstract Task<Transaction> CommitTransactionDeltaAsync(Transaction original, Transaction next);

        /// <summary>
        /// Commites the transaction
        /// </summary>
        /// <param name="original">The original for which the next is a revision of</param>
        /// <param name="next">The next revision of original</param>
        /// <returns>Committed transaction.</returns>
        public abstract Transaction CommitTransactionDelta(Transaction original, Transaction next);

        /// <summary>
        /// Commits a new transaction asynchronously
        /// </summary>
        /// <param name="transaction">The transaction o commit</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the committed transaction.</returns>
        public abstract Task<Transaction> CreateTransactionAsync(Transaction transaction);

        /// <summary>
        /// Fetches a transaction asynchronously
        /// </summary>
        /// <param name="id">The transaction ID</param>
        /// <param name="revision">The revision to fetch. Use -1 to specify the latest revision</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction.</returns>
        public abstract Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1);

        /// <summary>
        /// Saves the state of a transaction. This will store the serialized variable set produced by the script execution.
        /// </summary>
        /// <param name="id">The transaction id</param>
        /// <param name="revision">Revision to store state for</param>
        /// <param name="state">The state to store</param>
        public abstract void SaveTransactionState(Guid id, int revision, byte[] state);

        /// <summary>
        /// Saves the state of a transaction asynchronously. This will store the serialized variable set produced by the script execution.
        /// </summary>
        /// <param name="id">The transaction id</param>
        /// <param name="revision">Revision to store state for</param>
        /// <param name="state">The state to store</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task SaveTransactionStateAsync(Guid id, int revision, byte[] state);

        /// <summary>
        /// Frees a locked transaction
        /// </summary>
        /// <param name="id">The transaction to lock</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual Task FreeTransactionAsync(Guid id)
        {
            if (this.rowLock.TryGetValue(id, out var sem))
            {
                sem.Release();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Fetches an entire transaction chain asynchronously.
        /// </summary>
        /// <param name="id">The transaction chain to fetch</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction chain.</returns>
        public abstract Task<IEnumerable<Transaction>> GetChainAsync(Guid id);

        /// <summary>
        /// Fetches all child transactions for the specified transaction asynchronously.
        /// </summary>
        /// <param name="transaction">Transaction to fetch child transaction for</param>
        /// <param name="state">Child states to fetch</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction children.</returns>
        public abstract Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionState[] state);

        /// <summary>
        /// Asynchronously gets a value specifying whether the transaction has been locked.
        /// </summary>
        /// <param name="id">The transaction to check</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is true if the transaction is locked. Otherwise false.</returns>
        public virtual async Task<bool> IsTransactionLockedAsync(Guid id)
        {
            if (this.rowLock.TryGetValue(id, out var sem))
            {
                return !await sem.WaitAsync(0);
            }

            return false;
        }

        /// <summary>
        /// Frees up unused transaction locks.
        /// </summary>
        public void TransactionLockCleanup()
        {
            this.readerWriterLock.EnterWriteLock();
            try
            {
                var rows = this.rowLock.Keys.ToArray();

                foreach (var rowId in rows)
                {
                    this.rowLock.TryGetValue(rowId, out var sem);
                    if (sem.Wait(0))
                    {
                        sem.Release();
                        sem.Dispose();
                        this.rowLock.TryRemove(rowId, out sem);
                    }
                }
            }
            finally
            {
                this.readerWriterLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Locks a transaction asynchronously.
        /// </summary>
        /// <param name="id">The id of the transaction to lock</param>
        /// <param name="flags">Lock flags. These are currently unused.</param>
        /// <param name="timeout">Lock timeout. Defaults to inifinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task LockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            this.readerWriterLock.EnterReadLock();
            try
            {
                Task<bool> task = Task.FromResult(true);
                this.rowLock.AddOrUpdate(
                    id,
                    rowId =>
                    {
                        var res = new SemaphoreSlim(1);
                        task = res.WaitAsync(timeout);
                        return res;
                    },
                    (rowId, mut) =>
                    {
                        task = mut.WaitAsync(timeout);
                        return mut;
                    });
                if (!await task)
                {
                    throw new TransactionConflictException("The transaction was already locked.", id, 0);
                }
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a transaction query
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The query object is the result of the task.</returns>
        public abstract Task<IQueryable<Transaction>> QueryAsync();

        /// <summary>
        /// Asynchronously gets a value indicating whether the transaction exists or not.
        /// </summary>
        /// <param name="id">The transaction id to check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is whether the transaction exists or not.</returns>
        public abstract Task<bool> TransactionExistsAsync(Guid id);

        /// <summary>
        /// Tries to lock the transaction asynchronously.
        /// </summary>
        /// <param name="id">The id of the transaction to look</param>
        /// <param name="flags">Currently unused</param>
        /// <param name="timeout">The lock timeout. This defaults to inifinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is whether the lock was successful or not.</returns>
        public virtual async Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            this.readerWriterLock.EnterReadLock();
            try
            {
                Task<bool> task = Task.FromResult(true);
                this.rowLock.AddOrUpdate(
                    id,
                    rowId =>
                    {
                        var res = new SemaphoreSlim(1);
                        task = res.WaitAsync(timeout);
                        return res;
                    },
                    (rowId, mut) =>
                    {
                        task = mut.WaitAsync(timeout);
                        return mut;
                    });
                return await task;
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Opens the transaction storage asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task OpenAsync();

        /// <summary>
        /// Gets the serialized transaction state asynchronously.
        /// </summary>
        /// <param name="id">Id of the transaction to get state for</param>
        /// <param name="revision">Revision to get state for</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the serialized state of the transaciton.</returns>
        public abstract Task<byte[]> GetTransactionStateAsync(Guid id, int revision);

        /// <summary>
        /// Waits asynchronously until a predicate is true
        /// </summary>
        /// <param name="predicate">The wait predicate</param>
        /// <param name="timeout">The timeout. This defaults to infinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the transaction where predicate returned true.</returns>
        public virtual async Task<Transaction> WaitForAsync(Func<Transaction, bool> predicate, int timeout)
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

        /// <summary>
        /// Gets expiring transaction asynchronously.
        /// </summary>
        /// <param name="cancel">CancellationToken used to cancel task</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is all transactions that have currently expired.</returns>
        public async Task<List<Transaction>> GetExpiringTransactionsAsync(CancellationToken cancel)
        {
            if (await this.WaitForExpiringTransactions(cancel))
            {
                // If WaitForExpiringTransactions returns false, it either timed out or was cancelled.
                return await this.GetExpiringTransactionsInternal(cancel);
            }

            return this.empty;
        }

        /// <summary>
        /// This member is called when a transaction is committed to the <see cref="TransactionStorageBase"/>.
        /// </summary>
        /// <param name="transaction">The transaction that was committed</param>
        protected virtual void OnTransactionCommitted(Transaction transaction)
        {
            if (transaction.Expires != null && (this.nextExpiringTransaction == null || transaction.Expires < this.nextExpiringTransaction))
            {
                this.nextExpiringTransaction = transaction.Expires;
                this.nextExpiringTransactionChangedEvent.Set();
            }

            this.TransactionCommitted?.Invoke(this, new TransactionCommittedEventArgs(transaction));
        }

        /// <summary>
        /// Base class GetExpiringTransactions. This must be implemented by extenders.
        /// </summary>
        /// <param name="cancel">CancelationToken used to cancel</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is all transactions that have currently expired.</returns>
        protected abstract Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel);

        /// <summary>
        /// Sets the next expiration date
        /// </summary>
        /// <param name="next">Value to specify</param>
        protected void SetNextExpiringTransactionTime(DateTime? next)
        {
            this.nextExpiringTransaction = next;
        }

        /// <summary>
        /// Asynchronously waits for expiring transactions.
        /// </summary>
        /// <param name="cancel">The CancellationToken for the asynchronous operation</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is whether there were found expiring transactions or not.</returns>
        protected virtual async Task<bool> WaitForExpiringTransactions(CancellationToken cancel)
        {
            try
            {
                // This if-block looks like a race condition, but it *shouldn't* be.
                // The idea is that _nextExpiringTransactionChangedEvent will be open
                // or open soon if _nextExpiringTransaction changed in the meantime.
                // No other threads is supposed to open it.
                if (this.nextExpiringTransaction == null)
                {
                    return await this.nextExpiringTransactionChangedEvent.WaitOne(cancel);
                }

                int delta;
                do
                {
                    var deltaSpan = this.nextExpiringTransaction.Value - this.timeService.Now();
                    long deltaD = deltaSpan.Ticks / TimeSpan.TicksPerMillisecond;

                    if (deltaD < 0)
                    {
                        break;
                    }

                    delta = deltaD > int.MaxValue ? int.MaxValue : (int)deltaD;
                    if (delta > 0)
                    {
                        return await this.nextExpiringTransactionChangedEvent.WaitOne(delta, cancel);
                    }
                }
                while (delta > 0);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}
