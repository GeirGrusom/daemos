﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion
{
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
