// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    public class TransactionException : Exception
    {
        public Guid TransactionId { get; }

        public TransactionException(string message, Guid transactionId)
            : base(message)
        {
            this.TransactionId = transactionId;
        }

        public TransactionException(string message, Guid transactionId, Exception innerException)
    :       base(message, innerException)
        {
            this.TransactionId = transactionId;
        }
    }
}
