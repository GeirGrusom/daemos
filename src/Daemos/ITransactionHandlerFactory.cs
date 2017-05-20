namespace Daemos
{
    public interface ITransactionHandlerFactory
    {
        ITransactionHandler Get(string name);
    }
}