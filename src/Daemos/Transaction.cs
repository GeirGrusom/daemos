// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using Daemos.Scripting;

    /// <summary>
    /// This class implements an immutable transaction
    /// </summary>
    public sealed class Transaction : IEquatable<Transaction>, IEquatable<TransactionRevision>, ISerializable
    {
        private readonly TransactionData data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="id">Transaction id</param>
        /// <param name="revision">Revision of transaction id</param>
        /// <param name="created">When the transaction was created</param>
        /// <param name="expires">When the transaction expires and the script should be executed</param>
        /// <param name="expired">When the transaction actually expired</param>
        /// <param name="payload">Dynamic payload object</param>
        /// <param name="script">Script used to execute when transaction expires</param>
        /// <param name="status">Transaction status</param>
        /// <param name="parent">Parent transaction and revision. This can be null</param>
        /// <param name="error">Error associated with the transaction (if any)</param>
        /// <param name="storage">Storage engine that produced the transaction</param>
        public Transaction(Guid id, int revision, DateTime created, DateTime? expires, DateTime? expired, object payload, string script, TransactionStatus status, TransactionRevision? parent, object error, ITransactionStorage storage)
            : this(new TransactionData { Id = id, Revision = revision, Created = created, Expires = expires, Expired = expired, Parent = parent, Payload = payload, Script = script, Status = status, Error = error }, storage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="data">Base mutable data for transaction</param>
        /// <param name="storage">Storage engine that produced the transaction</param>
        public Transaction(TransactionData data, ITransactionStorage storage)
        {
            this.data = data;
            this.Storage = storage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="data">Reference to mutable data for transaction</param>
        /// <param name="storage">Storage engine that produced the transaction</param>
        public Transaction(ref TransactionData data, ITransactionStorage storage)
        {
            this.data = data;
            this.Storage = storage;
        }

        /// <summary>
        /// Gets a copy of the data used to initialize this transaction
        /// </summary>
        public TransactionData Data => this.data;

        /// <summary>
        /// Gets the storage engine that created this transaction
        /// </summary>
        public ITransactionStorage Storage { get; }

        /// <summary>
        /// Gets the id of the transaction
        /// </summary>
        public Guid Id => this.data.Id;

        /// <summary>
        /// Gets the revision for this transaction instance
        /// </summary>
        public int Revision => this.data.Revision;

        /// <summary>
        /// Gets the timestamp when this transaction was created
        /// </summary>
        public DateTime Created => this.data.Created;

        /// <summary>
        /// Gets a timestamp when the transaction will expire
        /// </summary>
        public DateTime? Expires => this.data.Expires;

        /// <summary>
        /// Gets a timestampe for when the transaction actually expired
        /// </summary>
        public DateTime? Expired => this.data.Expired;

        /// <summary>
        /// Gets a dynamic object payload for the transaction
        /// </summary>
        public object Payload => this.data.Payload;

        /// <summary>
        /// Gets the script associated with this transaction
        /// </summary>
        public string Script => this.data.Script;

        /// <summary>
        /// Gets the status of this transaction instance
        /// </summary>
        public TransactionStatus Status => this.data.Status;

        /// <summary>
        /// Gets the parent transaction id and revision
        /// </summary>
        public TransactionRevision? Parent => this.data.Parent;

        /// <summary>
        /// Gets an error object associated with this transaction
        /// </summary>
        public object Error => this.data.Error;

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            if (other is Transaction trans)
            {
                return this.Equals(trans);
            }

            if (other is TransactionRevision rev)
            {
                return this.Equals(rev);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(Transaction other)
        {
            return this.data.Equals(other.Data);
        }

        /// <inheritdoc/>
        public bool Equals(TransactionRevision other)
        {
            return this.Id == other.Id && this.Revision == other.Revision;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^ this.Revision.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Id:B}";
        }

        /// <summary>
        /// Serializes this transaction
        /// </summary>
        /// <param name="serializer">Serializer to write to</param>
        public void Serialize(IStateSerializer serializer)
        {
            serializer.Serialize(nameof(this.Id), this.Id);
            serializer.Serialize(nameof(this.Revision), this.Revision);
            serializer.Serialize(nameof(this.Created), this.Created);
            serializer.Serialize(nameof(this.Expires), this.Expires);
            serializer.Serialize(nameof(this.Expired), this.Expired);
            serializer.Serialize(nameof(this.Status), this.Status);
            serializer.Serialize(nameof(this.Script), this.Script);
            serializer.Serialize(nameof(this.Parent), this.Parent);
            serializer.Serialize(nameof(this.Payload), this.Payload);
            serializer.Serialize(nameof(this.Error), this.Error);
        }
    }
}
