using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion.Tests
{
    public delegate void TransactionMutationDelegate(ref TransactionData data);
    public static class TransactionExtensions
    {
        public static Transaction With(this Transaction transaction, TransactionMutationDelegate action)
        {
            var data = transaction.Data;

            action(ref data);

            return new Transaction(data, transaction.Storage);
        }
    }
}
