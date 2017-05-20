using System;
using System.Text;
using System.Threading.Tasks;

namespace Transact
{
    public interface ITransactionHandler
    {
        Task Cancel(Transaction transaction);
        Task Authorize(Transaction transaction);
        Task Complete(Transaction transaction);
        Task Fail(Transaction transaction);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class HandlerNameAttribute : Attribute
    {
        public string Name { get; }

        public HandlerNameAttribute(string name)
        {
            Name = name;
        }
    }

    [HandlerName("dummy")]
    public sealed class DummyTransactionHandler : TransactionHandler
    {
        public DummyTransactionHandler(ITransactionHandlerFactory factory, ITransactionStorage storage)
            : base(factory, storage)
        {
        }

    }

    public abstract class TransactionHandler : ITransactionHandler
    {
        public ITransactionStorage Storage { get; }
        public ITransactionHandlerFactory Factory { get; }

        protected TransactionHandler(ITransactionHandlerFactory factory, ITransactionStorage storage)
        {
            Storage = storage;
            Factory = factory;
        }

        public async Task Cancel(Transaction transaction)
        {
            var children = await Storage.GetChildTransactions(transaction.Id, TransactionState.Authorized, TransactionState.Initialized);

            foreach (var child in children)
            {
                var handler = Factory.Get(child.Handler);
                if (handler == null)
                    continue;
                await handler.Cancel(child);
            }

            var data = transaction.Data;
            data.State = TransactionState.Cancelled;
            data.Revision += 1;
            data.Script = null;
            data.Created = DateTime.UtcNow;
            var newTrans = new Transaction(data, Storage);
            await Storage.CommitTransactionDelta(transaction, newTrans);
        }

        public async Task Authorize(Transaction transaction)
        {
            var children = await Storage.GetChildTransactions(transaction.Id, TransactionState.Authorized, TransactionState.Initialized);

            foreach (var child in children)
            {
                if (child.State == TransactionState.Authorized)
                    continue;
                if (child.State != TransactionState.Initialized)
                {
                    throw new TransactionException("The child transaction {" + child.Id.ToString("N") + "} has not been initialized.", transaction.Id);
                }
                var handler = Factory.Get(child.Handler);
                if (handler == null)
                    continue;
                await handler.Authorize(child);
            }
            var data = transaction.Data;
            data.State = TransactionState.Authorized;
            data.Revision += 1;
            data.Script = null;
            data.Created = DateTime.UtcNow;
            var newTrans = new Transaction(data, Storage);
            await Storage.CommitTransactionDelta(transaction, newTrans);
        }

        public async Task Complete(Transaction transaction)
        {
            var children = await Storage.GetChildTransactions(transaction.Id, TransactionState.Authorized, TransactionState.Initialized);

            foreach (var child in children)
            {
                if (child.State == TransactionState.Completed)
                    continue;
                if (child.State != TransactionState.Authorized)
                {
                    throw new TransactionException("Could not complete transaction. The child transaction {" + child.Id.ToString("N") + "} has not been completed.", transaction.Id);
                }
                var handler = Factory.Get(child.Handler);
                if (handler == null)
                    continue;
                await handler.Complete(child);
            }

            var data = transaction.Data;
            data.Revision += 1;
            data.State = TransactionState.Completed;
            data.Script = null;
            data.Created = DateTime.UtcNow;
            var newTrans = new Transaction(data, Storage);
            await Storage.CommitTransactionDelta(transaction, newTrans);
        }

        public Task Fail(Transaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}
