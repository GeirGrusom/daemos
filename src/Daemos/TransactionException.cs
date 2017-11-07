// <copyright file="TransactionException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos
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
