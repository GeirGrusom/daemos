namespace Daemos.Tests
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

        public static Transaction IncrementRevision(this Transaction transaction)
        {
            var data = transaction.Data;
            data.Revision += 1;
            return new Transaction(data, transaction.Storage);
        }
    }
}
