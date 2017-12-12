// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface declares the transaction storage contract.
    /// </summary>
    public interface ITransactionStorage
    {
        /// <summary>
        /// This event is triggered when a transaction or a transaction revision is comitted.
        /// </summary>
        event EventHandler<TransactionCommittedEventArgs> TransactionCommitted;

        /// <summary>
        /// Gets the <see cref="ITimeService"/> used by this <see cref="TransactionStorageBase"/>.
        /// </summary>
        ITimeService TimeService { get; }

        /// <summary>
        /// Locks a transaction asynchronously.
        /// </summary>
        /// <param name="id">The id of the transaction to lock</param>
        /// <param name="flags">Lock flags. These are currently unused.</param>
        /// <param name="timeout">Lock timeout. Defaults to inifinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task LockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);

        /// <summary>
        /// Tries to lock the transaction asynchronously.
        /// </summary>
        /// <param name="id">The id of the transaction to look</param>
        /// <param name="flags">Currently unused</param>
        /// <param name="timeout">The lock timeout. This defaults to inifinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is whether the lock was successful or not.</returns>
        Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = Timeout.Infinite);

        /// <summary>
        /// Frees a locked transaction
        /// </summary>
        /// <param name="id">The transaction to lock</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task FreeTransactionAsync(Guid id);

        /// <summary>
        /// Asynchronously gets a value specifying whether the transaction has been locked.
        /// </summary>
        /// <param name="id">The transaction to check</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is true if the transaction is locked. Otherwise false.</returns>
        Task<bool> IsTransactionLockedAsync(Guid id);

        /// <summary>
        /// Asynchronously gets a value indicating whether the transaction exists or not.
        /// </summary>
        /// <param name="id">The transaction id to check.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is whether the transaction exists or not.</returns>
        Task<bool> TransactionExistsAsync(Guid id);

        /// <summary>
        /// Fetches a transaction asynchronously
        /// </summary>
        /// <param name="id">The transaction ID</param>
        /// <param name="revision">The revision to fetch. Use -1 to specify the latest revision</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction.</returns>
        Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1);

        /// <summary>
        /// Commits a new transaction asynchronously
        /// </summary>
        /// <param name="transaction">The transaction o commit</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the committed transaction.</returns>
        Task<Transaction> CreateTransactionAsync([NotNull] Transaction transaction);

        /// <summary>
        /// Commites the transaction asynchronously.
        /// </summary>
        /// <param name="original">The original for which the next is a revision of</param>
        /// <param name="next">The next revision of original</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the committed transaction.</returns>
        Task<Transaction> CommitTransactionDeltaAsync([NotNull] Transaction original, [NotNull] Transaction next);

        /// <summary>
        /// Commites the transaction
        /// </summary>
        /// <param name="original">The original for which the next is a revision of</param>
        /// <param name="next">The next revision of original</param>
        /// <returns>Committed transaction.</returns>
        Transaction CommitTransactionDelta([NotNull] Transaction original, [NotNull] Transaction next);

        /// <summary>
        /// Gets expiring transaction asynchronously.
        /// </summary>
        /// <param name="cancel">CancellationToken used to cancel task</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is all transactions that have currently expired.</returns>
        Task<List<Transaction>> GetExpiringTransactionsAsync(CancellationToken cancel);

        /// <summary>
        /// Fetches all child transactions for the specified transaction asynchronously.
        /// </summary>
        /// <param name="transaction">Transaction to fetch child transaction for</param>
        /// <param name="state">Child states to fetch</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction children.</returns>
        Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionState[] state);

        /// <summary>
        /// Opens the transaction storage asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task OpenAsync();

        /// <summary>
        /// Gets the serialized transaction state asynchronously.
        /// </summary>
        /// <param name="id">Id of the transaction to get state for</param>
        /// <param name="revision">Revision to get state for</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the serialized state of the transaciton.</returns>
        Task<byte[]> GetTransactionStateAsync(Guid id, int revision);

        /// <summary>
        /// Waits asynchronously until a predicate is true
        /// </summary>
        /// <param name="predicate">The wait predicate</param>
        /// <param name="timeout">The timeout. This defaults to infinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the transaction where predicate returned true.</returns>
        Task<Transaction> WaitForAsync(Func<Transaction, bool> predicate, int timeout = Timeout.Infinite);

        /// <summary>
        /// Fetches an entire transaction chain asynchronously.
        /// </summary>
        /// <param name="id">The transaction chain to fetch</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the fetched transaction chain.</returns>
        Task<IEnumerable<Transaction>> GetChainAsync(Guid id);

        /// <summary>
        /// Returns a transaction query
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The query object is the result of the task.</returns>
        Task<IQueryable<Transaction>> QueryAsync();

        /// <summary>
        /// Asynchronously initializes the <see cref="TransactionStorageBase"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Saves the state of a transaction. This will store the serialized variable set produced by the script execution.
        /// </summary>
        /// <param name="id">The transaction id</param>
        /// <param name="revision">Revision to store state for</param>
        /// <param name="state">The state to store</param>
        void SaveTransactionState(Guid id, int revision, byte[] state);
    }
}
