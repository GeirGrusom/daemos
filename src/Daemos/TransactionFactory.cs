// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Dynamic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Delegate used to mutate transactions
    /// </summary>
    /// <param name="data">Mutatation target</param>
    public delegate void MutateTransactionDataDelegate(ref TransactionMutableData data);

    /// <summary>
    /// Transaction lock flags
    /// </summary>
    [Flags]
    public enum LockFlags
    {
        /// <summary>
        /// No lock flags
        /// </summary>
        None = 0,

        /// <summary>
        /// Create transaction if it does not exist. This is not in use.
        /// </summary>
        CreateIfNotExists = 1
    }

    /// <summary>
    /// This class is a helper factory for creating new transactions using the specified storage.
    /// </summary>
    public sealed class TransactionFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionFactory"/> class.
        /// </summary>
        /// <param name="storage">Storage to create transactions for</param>
        public TransactionFactory(ITransactionStorage storage)
        {
            this.Storage = storage;
        }

        /// <summary>
        /// Gets the transaction storage for this instance.
        /// </summary>
        public ITransactionStorage Storage { get; }

        /// <summary>
        /// Asynchronously continues a transaction chain
        /// </summary>
        /// <param name="id">Id of transaction to continue</param>
        /// <param name="nextRevision">The expected next revision</param>
        /// <param name="timeout">Lock timeout. Defaults to infinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the continued transaction revision.</returns>
        public async Task<Transaction> ContinueTransactionAsync(Guid id, int nextRevision, int timeout = Timeout.Infinite)
        {
            var trans = await this.Storage.FetchTransactionAsync(id);
            if (!await trans.TryLock(timeout: timeout))
            {
                throw new TimeoutException("Could not get a lock on the transaction.");
            }

            if (nextRevision != trans.Revision + 1)
            {
                await trans.Free();
                throw new TransactionConflictException(id, nextRevision);
            }

            return trans;
        }

        /// <summary>
        /// Asynchronously continues a transaction chain
        /// </summary>
        /// <param name="id">Id of transaction to continue</param>
        /// <param name="timeout">Lock timeout. Defaults to infinite.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the continued transaction revision.</returns>
        public async Task<Transaction> ContinueTransactionAsync(Guid id, int timeout = Timeout.Infinite)
        {
            var trans = await this.Storage.FetchTransactionAsync(id);
            if (!await trans.TryLock(timeout: timeout))
            {
                throw new TimeoutException("Could not get a lock on the transaction.");
            }

            return trans;
        }

        /// <summary>
        /// Asynchronously starts a new transaction
        /// </summary>
        /// <param name="id">Id of transaction to start</param>
        /// <param name="expires">Expiration date for transaction</param>
        /// <param name="payload">Transaction payload</param>
        /// <param name="script">Script to run for transaction</param>
        /// <param name="parent">Parent transaction</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the new transaction.</returns>
        public async Task<Transaction> StartTransaction(Guid? id, DateTime? expires, object payload, string script, TransactionRevision? parent)
        {
            var trans = await this.Storage.CreateTransactionAsync(new Transaction(id ?? Guid.NewGuid(), 0, DateTime.UtcNow, expires, null, payload ?? new ExpandoObject(), script, TransactionState.Initialized, parent, null, this.Storage));
            await trans.Lock();
            return trans;
        }

        /// <summary>
        /// Asynchronously creates a new transaction without locking it.
        /// </summary>
        /// <param name="id">Id of transaction to start</param>
        /// <param name="expires">Expiration date for transaction</param>
        /// <param name="payload">Transaction payload</param>
        /// <param name="script">Script to run for transaction</param>
        /// <param name="parent">Parent transaction</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the new transaction.</returns>
        public async Task<Transaction> CreateTransaction(Guid? id, DateTime? expires, object payload, string script, TransactionRevision? parent)
        {
            return await this.Storage.CreateTransactionAsync(new Transaction(id ?? Guid.NewGuid(), 0, DateTime.UtcNow, expires, null, payload ?? new ExpandoObject(), script, TransactionState.Initialized, parent, null, this.Storage));
        }
    }
}
