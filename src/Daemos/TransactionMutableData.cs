// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// Represents user mutable data for a transaction
    /// </summary>
    public struct TransactionMutableData
    {
        /// <summary>
        /// Gets or sets an expiration timestamp for this transaction
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Gets or sets the payload for this transaction
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Gets or sets the script for this transaction
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets the status of this transaction
        /// </summary>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error object associated with this transaction
        /// </summary>
        public object Error { get; set; }
    }
}
