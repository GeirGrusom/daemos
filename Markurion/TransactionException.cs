using System;

namespace Markurion
{
    public class TransactionException : Exception
    {
        public Guid TransactionId { get; }

        public TransactionException(string message, Guid transactionId)
            : base(message)
        {
            TransactionId = transactionId;
        }

        public TransactionException(string message, Guid transactionId, Exception innerException)
    :       base(message, innerException)
        {
            TransactionId = transactionId;
        }
    }
}
