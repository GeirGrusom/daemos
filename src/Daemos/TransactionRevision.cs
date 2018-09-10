// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This data type represents a transaction id with a revision
    /// </summary>
    public struct TransactionRevision : IEquatable<TransactionRevision>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRevision"/> struct.
        /// </summary>
        /// <param name="id">Specifies the id of this reference</param>
        /// <param name="revision">Specifies the revision for this reference</param>
        public TransactionRevision(Guid id, int revision)
        {
            this.Id = id;
            this.Revision = revision;
        }

        /// <summary>
        /// Gets the id of this reference
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the revision of this reference
        /// </summary>
        public int Revision { get; }

        /// <summary>
        /// Implements equality comparison for transaction revisions
        /// </summary>
        /// <param name="lhs">Left hand revision</param>
        /// <param name="rhs">Right hand revision</param>
        /// <returns>True if the revisions are equal. Otherwise false.</returns>
        public static bool operator ==(TransactionRevision lhs, TransactionRevision rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Implements inequality comparison for transaction revisions
        /// </summary>
        /// <param name="lhs">Left hand revision</param>
        /// <param name="rhs">Right hand revision</param>
        /// <returns>True if the revisions are not equal. Otherwise false.</returns>
        public static bool operator !=(TransactionRevision lhs, TransactionRevision rhs)
        {
            return !(lhs == rhs);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is TransactionRevision && this.Equals((TransactionRevision)obj);
        }

        /// <inheritdoc/>
        public bool Equals(TransactionRevision other)
        {
            return this.Id.Equals(other.Id) &&
                   this.Revision == other.Revision;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1532668728;
            hashCode = (hashCode * -1521134295) + base.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<Guid>.Default.GetHashCode(this.Id);
            hashCode = (hashCode * -1521134295) + this.Revision.GetHashCode();
            return hashCode;
        }
    }
}
