// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.Scripting;

    /// <summary>
    /// Processes transactions from a <see cref="ITransactionStorage"/>.
    /// </summary>
    public class TransactionProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionProcessor"/> class.
        /// </summary>
        /// <param name="storage">The storage used to retrieve transactions from.</param>
        /// <param name="scriptRunner">The script runner used to execute scripts with.</param>
        /// <param name="container">The container used to resolve script dependencies with.</param>
        public TransactionProcessor(ITransactionStorage storage, IScriptRunner scriptRunner, IContainer container)
        {
            this.Storage = storage;
            this.ScriptRunner = scriptRunner;
            this.Container = container;
        }

        /// <summary>
        /// Gets the storage used to retrieve pending transactions from.
        /// </summary>
        public ITransactionStorage Storage { get; }

        /// <summary>
        /// Gets the script runner used to execute scripts on transactions.
        /// </summary>
        public IScriptRunner ScriptRunner { get; }

        /// <summary>
        /// Gets the container used as a root execution container. This is used to resolve any dependencies the script might have.
        /// </summary>
        public IContainer Container { get; }

        /// <summary>
        /// Runs the transaction processor as an asynchronous task
        /// </summary>
        /// <param name="cancel">Cancellation token used to stop processing.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(CancellationToken cancel)
        {
            Console.WriteLine("Starting transaction processing.");
            while (!cancel.IsCancellationRequested)
            {
                var transactions = await this.Storage.GetExpiringTransactionsAsync(cancel);
                int count = 0;
                foreach (var transaction in transactions)
                {
                    if (!await transaction.TryLock(timeout: 0))
                    {
                        continue;
                    }

                    ++count;

                    var dependencyResolver = this.Container.CreateProxy();

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
                            this.ScriptRunner.Run(transaction.Script, dependencyResolver);
                        }
                        else
                        {
                            await transaction.CreateDelta(transaction.Revision + 1, true, (ref TransactionMutableData x) =>
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
                                transaction.CreateDelta(
                                    transaction.Revision + 1,
                                    true,
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
                                this.Storage);

                            await this.Storage.CreateTransactionAsync(trans);
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
