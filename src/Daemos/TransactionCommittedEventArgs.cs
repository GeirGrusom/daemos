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
