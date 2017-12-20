// <copyright file="TransactionFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System;

    public static class TransactionFactory
    {
        public static Transaction CreateNew(ITransactionStorage storage)
        {
            return new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionStatus.Initialized, null, null, storage);
        }
    }
}
