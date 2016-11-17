namespace Markurion.Api.Models
{
    public static class TransactionMapper
    {
        public static TransactionResult ToTransactionResult(this Transaction input)
        {
            return new TransactionResult(input);
        }
    }
}
