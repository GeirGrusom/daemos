// <copyright file="TransactionCommittedEventArgs.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos
{
    public sealed class TransactionCommittedEventArgs : EventArgs
    {
        public Transaction Transaction { get; }

        public TransactionCommittedEventArgs(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}
