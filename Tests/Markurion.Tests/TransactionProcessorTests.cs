using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Markurion.Tests
{
    using Scripting;
    public class TransactionProcessorTests
    {

        public class Service
        {
            public ITransactionStorage Storage { get; }
            public IScriptRunner ScriptRunner { get; }
            public TransactionProcessor Processor { get; }
            public IContainer Container { get; }

            public Service()
            {
                Storage = Substitute.For<ITransactionStorage>();
                ScriptRunner = Substitute.For<IScriptRunner>();
                Container = Substitute.For<IContainer>();
                Container.CreateProxy().Returns(Container);
                Processor = new TransactionProcessor(Storage, ScriptRunner, Container);
            }

            [DebuggerStepThrough]
            public Task RunAsync()
            {
                var cancellation = new CancellationTokenSource();
                cancellation.CancelAfter(100);
                return Processor.RunAsync(cancellation.Token);
            }
        }

        [Fact]
        public async Task RetrievesExpiringTransactions()
        {
            // Arrange
            var service = new Service();
            service.Storage.GetExpiringTransactionsAsync(Arg.Any<CancellationToken>()).Returns(new List<Transaction>());

            // Act
            await service.RunAsync();

            // Assert
            await service.Storage.Received().GetExpiringTransactionsAsync(Arg.Any<CancellationToken>());
        }
    }
}
