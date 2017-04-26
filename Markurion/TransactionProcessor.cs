using System;
using System.Threading;
using System.Threading.Tasks;

namespace Markurion
{
    using Scripting;
    public class TransactionProcessor
    {

        public ITransactionStorage Storage { get; }
        public IScriptRunner ScriptRunner { get; }

        public IContainer Container { get; }

        public TransactionProcessor(ITransactionStorage storage, IScriptRunner scriptRunner, IContainer container )
        {
            Storage = storage;
            ScriptRunner = scriptRunner;
            Container = container;
        }

        public async Task RunAsync(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                var transactions = await Storage.GetExpiringTransactionsAsync(cancel);
                int count = 0;
                foreach (var transaction in transactions)
                {
                    if (!await transaction.TryLock(timeout: 0))
                        continue;
                    
                    ++count;

                    var dependencyResolver = Container.CreateProxy();
                    dependencyResolver.Register(transaction);
                    dependencyResolver.Register<IStateDeserializer>(new StateDeserializer());
                    dependencyResolver.Register<IStateSerializer>(new StateSerializer());
                    try
                    {
                        if (!string.IsNullOrEmpty(transaction.Script))
                        {
                            ScriptRunner.Run(transaction.Script, dependencyResolver);
                            /*await transaction.CreateDelta(transaction.Revision + 1, true, (ref TransactionMutableData x) =>
                            {
                                x = nextData;
                            });*/
                        }
                        else
                        {
                            await transaction.CreateDelta(transaction.Revision + 1, true,(ref TransactionMutableData x) =>
                            {
                                x.Script = null;
                                x.Expires = null;
                            });
                        }                           
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await
                                transaction.CreateDelta(transaction.Revision + 1, true,
                                    (ref TransactionMutableData x) =>
                                    {
                                        x.State = TransactionState.Failed;
                                        x.Payload = transaction.Payload;
                                        x.Error = ex;
                                        x.Expires = null;
                                        x.Script = transaction.Script;
                                    });
                        }
                        catch (Exception unlikelyException)
                        {
                            // Probably optimistic concurrency failed.
                            // We lost our chance to create a delta,
                            // so let's make a new transaction that links to where we failed.
                            var trans = new Transaction(
                                Guid.NewGuid(), 
                                0, 
                                DateTime.UtcNow, 
                                null, 
                                null, 
                                new { failure = "optimistic" }, 
                                null,
                                TransactionState.Failed,
                                new TransactionRevision(transaction.Id, transaction.Revision), 
                                unlikelyException, 
                                Storage);

                            await Storage.CreateTransactionAsync(trans);
                        }
                    }
                    finally
                    {
                        await transaction.Free();
                    }
                }
                System.Diagnostics.Debug.WriteLine($"Processed {count} transactions.");
                
                await Task.Yield();
            }
        }
    }
}

