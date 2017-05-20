namespace Transact
{
    public interface ITransactionHandlerFactory
    {
        ITransactionHandler Get(string name);
    }
}