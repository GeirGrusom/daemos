using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion
{
    public sealed class TransactionRevisionExistsException : TransactionException
    {
        public int Revision { get; }
        public TransactionRevisionExistsException(Guid transactionId, int revision)
            : base("The specified transaction already exists.", transactionId)
        {
            Revision = revision;
        }

        public TransactionRevisionExistsException(Guid transactionId, int revision, Exception innerException)
            : base("The specified transaction already exists.", transactionId, innerException)
        {
            Revision = revision;
        }

        public TransactionRevisionExistsException(string message, Guid transactionId, int revision, Exception innerException)
            : base(message, transactionId, innerException)
        {
            Revision = revision;
        }
    }
}
