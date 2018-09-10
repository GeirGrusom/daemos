// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// Represents mutable transaction data
    /// </summary>
    public struct TransactionData : IEquatable<TransactionData>
    {
        /// <summary>
        /// Gets or sets the id of this transaction
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the revision of this transaction
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this transaction was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets an expiration timestamp for this transaction
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Gets or sets a timestamp for when this transaction expired
        /// </summary>
        public DateTime? Expired { get; set; }

        /// <summary>
        /// Gets or sets a payload for this transaction
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// Gets or sets a script for this transaction
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Gets or sets a parent transaction with revision for this transaction
        /// </summary>
        public TransactionRevision? Parent { get; set; }

        /// <summary>
        /// Gets or sets the status of this transaction
        /// </summary>
        public TransactionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets an error object associated with this transaction
        /// </summary>
        public object Error { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            if (other is TransactionData data)
            {
                return this.Equals(data);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <inheritdoc/>
        public bool Equals(TransactionData other)
        {
            return
                this.Id == other.Id &&
                this.Revision == other.Revision &&
                this.Created == other.Created &&
                this.Expires == other.Expires &&
                this.Expired == other.Expired &&
                this.Payload == other.Payload &&
                this.Script == other.Script &&
                this.Status == other.Status &&
                Equals(this.Error, other.Error);
        }
    }
}
