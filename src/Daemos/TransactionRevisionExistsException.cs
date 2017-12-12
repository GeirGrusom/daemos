// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    public sealed class TransactionRevisionExistsException : TransactionException
    {
        public int Revision { get; }

        public TransactionRevisionExistsException(Guid transactionId, int revision)
            : base("The specified transaction already exists.", transactionId)
        {
            this.Revision = revision;
        }

        public TransactionRevisionExistsException(Guid transactionId, int revision, Exception innerException)
            : base("The specified transaction already exists.", transactionId, innerException)
        {
            this.Revision = revision;
        }

        public TransactionRevisionExistsException(string message, Guid transactionId, int revision, Exception innerException)
            : base(message, transactionId, innerException)
        {
            this.Revision = revision;
        }
    }
}
