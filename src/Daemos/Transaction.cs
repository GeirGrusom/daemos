// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using Daemos.Scripting;
    using System;

    public enum TransactionStatus
    {
        Initialized,
        Authorized,
        Completed,
        Cancelled,
        Failed
    }

    public struct TransactionRevision
    {
        public TransactionRevision(Guid id, int revision)
        {
            this.Id = id;
            this.Revision = revision;
        }

        public Guid Id { get; }

        public int Revision { get; }
    }

    public struct TransactionData : IEquatable<TransactionData>
    {
        public Guid Id { get; set; }

        public int Revision { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Expires { get; set; }

        public DateTime? Expired { get; set; }

        public object Payload { get; set; }

        public string Script { get; set; }

        public TransactionRevision? Parent { get; set; }

        public TransactionStatus Status { get; set; }

        public string Handler { get; set; }

        public object Error { get; set; }

        public override bool Equals(object other)
        {
            if (other is TransactionData data)
            {
                return this.Equals(data);
            }

            return false;
        }

        public override int GetHashCode() => this.Id.GetHashCode();

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
                //Parent == other.Parent &&
                this.Status == other.Status &&
                this.Handler == other.Handler &&
                Equals(this.Error, other.Error);
        }
    }

    public struct TransactionMutableData
    {
        public DateTime? Expires { get; set; }

        public object Payload { get; set; }

        public string Script { get; set; }

        public TransactionStatus State { get; set; }

        public string Handler { get; set; }

        public object Error { get; set; }
    }

    public sealed class Transaction : IEquatable<Transaction>, IEquatable<TransactionRevision>, ISerializable
    {
        public Transaction(Guid id, int revision, DateTime created, DateTime? expires, DateTime? expired, object payload, string script, TransactionStatus state, TransactionRevision? parent, object error, ITransactionStorage storage)
            : this(new TransactionData { Id = id, Revision  = revision, Created = created, Expires = expires, Expired = expired, Parent = parent, Payload = payload, Script = script, Status = state, Error = error }, storage)
        {
        }

        public Transaction(TransactionData data, ITransactionStorage storage)
        {
            this.data = data;
            this.Storage = storage;
        }

        public Transaction(ref TransactionData data, ITransactionStorage storage)
        {
            this.data = data;
            this.Storage = storage;
        }

        private readonly TransactionData data;

        public TransactionData Data => this.data;

        public ITransactionStorage Storage { get; }

        public Guid Id => this.data.Id;

        public int Revision => this.data.Revision;

        public DateTime Created => this.data.Created;

        public DateTime? Expires => this.data.Expires;

        public DateTime? Expired => this.data.Expired;

        public object Payload => this.data.Payload;

        public string Script => this.data.Script;

        public TransactionStatus Status => this.data.Status;

        public TransactionRevision? Parent => this.data.Parent;

        public object Error => this.data.Error;

        public string Handler => this.data.Handler;

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

        public bool Equals(Transaction other)
        {
            return this.data.Equals(other.Data);
        }

        public bool Equals(TransactionRevision other)
        {
            return this.Id == other.Id && this.Revision == other.Revision;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^ this.Revision.GetHashCode();
        }

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

        /*public static bool operator ==(Transaction lhs, Transaction rhs)
        {
            return lhs.Id == rhs.Id && lhs.Revision == rhs.Revision;
        }

        public static bool operator !=(Transaction lhs, Transaction rhs)
        {
            return lhs.Id != rhs.Id || lhs.Revision != rhs.Revision;
        }*/
    }
}
