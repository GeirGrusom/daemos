// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// This struct represents data for a new child transaction
    /// </summary>
    public struct NewChildTransactionData
    {
        /// <summary>
        /// Gets or sets the Id of the child transaction
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the child transaction
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Gets or sets the payload for the child transaction
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Gets or sets the script for the child transaction
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets the error object for the child transaction
        /// </summary>
        public object Error { get; set; }
    }
}
