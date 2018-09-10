// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension functions for <see cref="Transaction"/>.
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// Locks a transaction
        /// </summary>
        /// <param name="scope">The transaction to lock</param>
        /// <param name="flags">Specifies how the lock is held</param>
        /// <param name="timeout">Timeout to wait for a lock. The default is infinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Lock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.LockTransactionAsync(scope.Id, flags, timeout);
        }

        /// <summary>
        /// Tries to lock a transaction
        /// </summary>
        /// <param name="scope">The transaction to lock</param>
        /// <param name="flags">Specifies how the lock is held</param>
        /// <param name="timeout">Timeout to wait for a lock. The default is infinite</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The task result is true if the lock was captured. Otherwise false.</returns>
        public static Task<bool> TryLock(this Transaction scope, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite)
        {
            return scope.Storage.TryLockTransactionAsync(scope.Id, flags, timeout);
        }

        /// <summary>
        /// Frees a locked transaction
        /// </summary>
        /// <param name="transaction">Transaction to free</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Free(this Transaction transaction)
        {
            return transaction.Storage.FreeTransactionAsync(transaction.Id);
        }

        /// <summary>
        /// Creates a new transaction by using a delta operation
        /// </summary>
        /// <param name="original">Original transaction</param>
        /// <param name="nextRevision">Next revision number</param>
        /// <param name="expired">Specifies if the transaction has expired</param>
        /// <param name="delta">Mutation delegate</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The new transaction is the result of the asynchronous operation.</returns>
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
                Status = data.Status
            };
            delta(ref mutableData);

            data.Expires = mutableData.Expires;
            data.Script = mutableData.Script;
            data.Payload = mutableData.Payload;
            data.Status = mutableData.Status;
            data.Error = mutableData.Error;

            var newTransaction = new Transaction(data, original.Storage);

            return original.Storage.CommitTransactionDeltaAsync(original, newTransaction);
        }

        /// <summary>
        /// Fetches a transaction from storage using the specified revision.
        /// </summary>
        /// <param name="scope">Transaction to fetch</param>
        /// <param name="revision">Revision to use. The default is to use the latest revision</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The asynchronous operation retutns the fetched transaction.</returns>
        public static Task<Transaction> Fetch(this Transaction scope, int revision = -1)
        {
            return scope.Storage.FetchTransactionAsync(scope.Id, revision);
        }
    }
}
