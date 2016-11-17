namespace Markurion
{
    public interface ITransactionHandlerFactory
    {
        ITransactionHandler Get(string name);
    }
}