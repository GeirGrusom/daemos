// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    public sealed class TransactionExistsException : TransactionException
    {
        public TransactionExistsException(Guid transactionId)
            : base("The specified transaction already exists.", transactionId)
        {
        }

        public TransactionExistsException(Guid transactionId, Exception innerException)
            : base("The specified transaction already exists.", transactionId, innerException)
        {
        }

        public TransactionExistsException(string message, Guid transactionId, Exception innerException)
            : base(message, transactionId, innerException)
        {
        }
    }
}
