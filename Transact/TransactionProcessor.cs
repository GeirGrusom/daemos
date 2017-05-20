using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Transact
{
    public class TransactionProcessor
    {

        public ITransactionStorage Storage { get; }
        public IScriptRunner ScriptRunner { get; }

        public TransactionProcessor(ITransactionStorage storage, IScriptRunner scriptRunner)
        {
            Storage = storage;
            ScriptRunner = scriptRunner;
        }

        public async Task RunAsync(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var transactions = await Storage.GetExpiringTransactions(now, cancel);
                int count = 0;
                foreach (var transaction in transactions)
                {
                    if (!await transaction.TryLock(timeout: 0))
                        continue;
                    
                    ++count;


                    try
                    {
                        if (transaction.Expires < now)
                        {

                            if (!string.IsNullOrEmpty(transaction.Script))
                            {
                                var nextData = await ScriptRunner.Run(transaction.Script, transaction);
                                await transaction.CreateDelta(transaction.Revision + 1, true, (ref TransactionMutableData x) =>
                                {
                                    x = nextData;
                                });
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

                            await Storage.CreateTransaction(trans);
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

