using System;

namespace Daemos.Tests
{
    public static class TransactionFactory
    {
        public static Transaction CreateNew(ITransactionStorage storage)
        {
            return new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null, storage);
        }
    }
}
