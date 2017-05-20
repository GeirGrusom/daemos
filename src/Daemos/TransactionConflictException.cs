using System;

namespace Daemos
{
    public class TransactionConflictException : TransactionException
    {
        public int Revision { get; }

        public TransactionConflictException(string message, Guid transaction, int revision)
            : base(message, transaction)
        {
            Revision = revision;
        }

        public TransactionConflictException(Guid transaction, int revision)
            : this("There was a conflict with the specified transaction id and revision.", transaction, revision)
        {
        }

    }
}
