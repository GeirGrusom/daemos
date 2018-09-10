// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    /// <summary>
    /// Defines statuses for a transaction
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// The transaction is in an initialization status. This is used to set initial properties on the transaction such as payload and child transactions.
        /// </summary>
        Initialized,

        /// <summary>
        /// The transaction is in an authorized state. This is used to indicate that the transaction is ready for completion.
        /// </summary>
        Authorized,

        /// <summary>
        /// The transaction is in a completed status. This is used when the transaction and its children have processed all information and is considered final.
        /// </summary>
        Completed,

        /// <summary>
        /// The transaction was cancelled by direct or indirect user interaction.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The transaction has failed. Details should be available in the Error property of the transaction.
        /// </summary>
        Failed
    }
}
