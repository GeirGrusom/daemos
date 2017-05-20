﻿using System;

namespace Daemos
{

    public enum TransactionState
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
            Id = id;
            Revision = revision;
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
        public TransactionState State { get; set; }
        public string Handler { get; set; }
        public object Error { get; set; }

        public override bool Equals(object other)
        {
            if(other is TransactionData data)
            {
                return Equals(data);
            }
            return false;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public bool Equals(TransactionData other)
        {
            return
                Id == other.Id &&
                Revision == other.Revision &&
                Created == other.Created &&
                Expires == other.Expires &&
                Expired == other.Expired &&
                Payload == other.Payload &&
                Script == other.Script &&
                //Parent == other.Parent &&
                State == other.State &&
                Handler == other.Handler &&
                Equals(Error, other.Error);
        }
    }

    public struct TransactionMutableData
    { 
        public DateTime? Expires { get; set; }
        public object Payload { get; set; }

        public string Script { get; set; }
        public TransactionState State { get; set; }
        public string Handler { get; set; }
        public object Error { get; set; }
    }


    public sealed class Transaction : IEquatable<Transaction>, IEquatable<TransactionRevision>
    {

        public Transaction(Guid id, int revision, DateTime created, DateTime? expires, DateTime? expired, object payload, string script, TransactionState state, TransactionRevision? parent, object error, ITransactionStorage storage)
            : this(new TransactionData { Id = id, Revision  = revision, Created = created, Expires = expires, Expired = expired, Parent = parent, Payload = payload, Script = script, State = state, Error = error }, storage)
        {
        }

        public Transaction(TransactionData data, ITransactionStorage storage)
        {
            _data = data;
            Storage = storage;
        }

        public Transaction(ref TransactionData data, ITransactionStorage storage)
        {
            _data = data;
            Storage = storage;
        }

        private readonly TransactionData _data;

        public TransactionData Data => _data;

        public ITransactionStorage Storage { get; }

        public Guid Id => _data.Id;
        public int Revision => _data.Revision;
        public DateTime Created => _data.Created;
        public DateTime? Expires => _data.Expires;
        public DateTime? Expired => _data.Expired;
        public object Payload => _data.Payload;
        public string Script => _data.Script;
        public TransactionState State => _data.State;
        public TransactionRevision? Parent => _data.Parent;
        public object Error => _data.Error;
        public string Handler => _data.Handler;

        public override bool Equals(object other)
        {
            if(other is Transaction trans)
            {
                return Equals(trans);
            }
            if(other is TransactionRevision rev)
            {
                return Equals(rev);
            }
            return false;
        }

        public bool Equals(Transaction other)
        {
            return _data.Equals(other.Data);
        }

        public bool Equals(TransactionRevision other)
        {
            return Id == other.Id && Revision == other.Revision;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Revision.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Id:B}";
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