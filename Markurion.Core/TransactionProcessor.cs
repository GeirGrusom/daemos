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
                var transactions = Storage.GetExpiringTransactions(DateTime.UtcNow, cancel).ToArray();
                int count = 0;
                foreach (var transaction in transactions)
                {
                    if (!await transaction.TryLock(timeout: 0))
                        continue;
                    
                    ++count;
                    var expiredTransaction = await transaction.Expire();
                    try
                    {
                        if (transaction.Expires < now)
                        {
                            if (!string.IsNullOrEmpty(transaction.Script))
                            {
                                await ScriptRunner.Run(transaction.Script, expiredTransaction, transaction);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await expiredTransaction.CreateDelta((ref TransactionMutableData x) =>
                        {
                            x.State = TransactionState.Failed;
                            x.Payload = ex;
                            x.Expires = null;
                            x.Script = null;
                        });
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
