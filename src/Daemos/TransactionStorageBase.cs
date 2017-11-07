// <copyright file="TransactionStorageBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Daemos
{
    public abstract class TransactionStorageBase : ITransactionStorage
    {
        public event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        private readonly Threading.AutoResetEvent _nextExpiringTransactionChangedEvent;
        private DateTime? _nextExpiringTransaction;
        private readonly ITimeService _timeService;

        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _rowLock;
        private readonly ReaderWriterLockSlim _readerWriterLock;


        public ITimeService TimeService => _timeService;

        protected TransactionStorageBase()
            : this(new UtcTimeService())
        {
        }

        protected TransactionStorageBase(ITimeService timeService)
        {
            _rowLock = new ConcurrentDictionary<Guid, SemaphoreSlim>();
            _readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _nextExpiringTransactionChangedEvent = new Threading.AutoResetEvent(true);
            _timeService = timeService;
        }


        protected virtual void OnTransactionCommitted(Transaction transaction)
        {
            if (transaction.Expires != null && (_nextExpiringTransaction == null || transaction.Expires < _nextExpiringTransaction))
            {
                _nextExpiringTransaction = transaction.Expires;
                _nextExpiringTransactionChangedEvent.Set();
            }
            TransactionCommitted?.Invoke(this, new TransactionCommittedEventArgs(transaction));
        }

        public abstract Task InitializeAsync();

        public abstract Task<Transaction> CommitTransactionDeltaAsync(Transaction original, Transaction next);

        public abstract Transaction CommitTransactionDelta(Transaction original, Transaction next);

        public abstract Task<Transaction> CreateTransactionAsync(Transaction transaction);

        public abstract Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1);

        public abstract void SaveTransactionState(Guid id, int revision, byte[] state);

        public abstract Task SaveTransactionStateAsync(Guid id, int revision, byte[] state);

        public virtual Task FreeTransactionAsync(Guid id)
        {
            if (_rowLock.TryGetValue(id, out var sem))
            {
                sem.Release();
            }
            return Task.CompletedTask;
        }

        public abstract Task<IEnumerable<Transaction>> GetChainAsync(Guid id);

        public abstract Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionState[] state);

        public virtual async Task<bool> IsTransactionLockedAsync(Guid id)
        {
            if (_rowLock.TryGetValue(id, out var sem))
            {
                return !await sem.WaitAsync(0);
            }
            return false;
        }

        public void TransactionLockCleanup()
        {
            _readerWriterLock.EnterWriteLock();
            try
            {
                var rows = _rowLock.Keys.ToArray();

                foreach (var rowId in rows)
                {
                    _rowLock.TryGetValue(rowId, out var sem);
                    if (sem.Wait(0))
                    {
                        sem.Release();
                        sem.Dispose();
                        _rowLock.TryRemove(rowId, out sem);
                    }
                }
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
        }

        public virtual async Task LockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                Task<bool> task = Task.FromResult(true);
                _rowLock.AddOrUpdate(id, rowId =>
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
                _readerWriterLock.ExitReadLock();
            }
        }


        public abstract Task<IQueryable<Transaction>> QueryAsync();

        public abstract Task<bool> TransactionExistsAsync(Guid id);

        public virtual async Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                Task<bool> task = Task.FromResult(true);
                _rowLock.AddOrUpdate(id, rowId =>
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
                _readerWriterLock.ExitReadLock();
            }
        }

        public abstract Task OpenAsync();

        public abstract Task<byte[]> GetTransactionStateAsync(Guid id, int revision);

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
                    TransactionCommitted -= ev;
                    sem.Release();
                }
            };

            TransactionCommitted += ev;

            if (!await sem.WaitAsync(timeout))
                return null;

            return result;
        }

        private readonly List<Transaction> Empty = new List<Transaction>(0);

        public async Task<List<Transaction>> GetExpiringTransactionsAsync(CancellationToken cancel)
        {
            if (await WaitForExpiringTransactions(cancel))
            {
                // If WaitForExpiringTransactions returns false, it either timed out or was cancelled.
                return await GetExpiringTransactionsInternal(cancel);
            }
            return Empty;
        }

        protected abstract Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel);

        protected void SetNextExpiringTransactionTime(DateTime? next)
        {
            _nextExpiringTransaction = next;
        }

        protected virtual async Task<bool> WaitForExpiringTransactions(CancellationToken cancel)
        {
            try
            {
                // This if-block looks like a race condition, but it *shouldn't* be.
                // The idea is that _nextExpiringTransactionChangedEvent will be open
                // or open soon if _nextExpiringTransaction changed in the meantime.
                // No other threads is supposed to open it.
                if (_nextExpiringTransaction == null)
                {
                    return await _nextExpiringTransactionChangedEvent.WaitOne(cancel);
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
                        return await _nextExpiringTransactionChangedEvent.WaitOne(delta, cancel);
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
