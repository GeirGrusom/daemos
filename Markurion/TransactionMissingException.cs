using System;

namespace Markurion
{
    public sealed  class TransactionMissingException : TransactionException
    {
        public TransactionMissingException(Guid transactionId) : this("The specified transaction does not exist.", transactionId)
        {
        }

        public TransactionMissingException(Guid transactionId, Exception innerException) : base("The specified transaction does not exist.", transactionId, innerException)
        {
        }

        public TransactionMissingException(string message, Guid transactionId) : base(message, transactionId)
        {
        }

        public TransactionMissingException(string message, Guid transactionId, Exception innerException) : base(message, transactionId, innerException)
        {
        }
    }
}
