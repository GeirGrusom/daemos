﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Daemos.Scripting;

namespace Daemos
{
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
            Console.WriteLine("Starting transaction processing.");
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

                    var proxyTransactionData = transaction.Data;
                    proxyTransactionData.Created = DateTime.UtcNow;
                    proxyTransactionData.Expired = proxyTransactionData.Expires ?? proxyTransactionData.Created;
                    proxyTransactionData.Expires = null;
                    var state = await transaction.Storage.GetTransactionStateAsync(transaction.Id, transaction.Revision);

                    dependencyResolver.Register(new Transaction(proxyTransactionData, transaction.Storage));
                    dependencyResolver.Register<IStateDeserializer>(new StateDeserializer(state));
                    dependencyResolver.Register<IStateSerializer>(new StateSerializer());
                    dependencyResolver.Register<ITimeService>(new ConstantTimeService(proxyTransactionData.Created), "now");
                    dependencyResolver.Register<ITimeService>(new ConstantTimeService(transaction.Expires ?? proxyTransactionData.Created), "expired");

                    Console.WriteLine($"Processing transaction {transaction.Id:N}[{transaction.Revision}]...");
                    try
                    {
                        if (!string.IsNullOrEmpty(transaction.Script))
                        {
                            ScriptRunner.Run(transaction.Script, dependencyResolver);
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
                        object error;
                        if (ex is CompilationFailedException cex)
                        {
                            error = SerializedCompilationError.FromException(cex);
                        }
                        else
                        {
                            error = SerializedException.FromException(ex);
                        }
                        try
                        {
                            await
                                transaction.CreateDelta(transaction.Revision + 1, true,
                                    (ref TransactionMutableData x) =>
                                    {
                                        x.State = TransactionState.Failed;
                                        x.Payload = transaction.Payload;
                                        x.Error = error;
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
                                SerializedException.FromException(unlikelyException), 
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
            Console.WriteLine("Transaction processing stopped.");
        }
    }
}

