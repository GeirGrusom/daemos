// <copyright file="TransactionProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.Scripting;
    using NSubstitute;
    using Xunit;

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
                this.Storage = Substitute.For<ITransactionStorage>();
                this.ScriptRunner = Substitute.For<IScriptRunner>();
                this.Container = Substitute.For<IContainer>();
                this.Container.CreateProxy().Returns(this.Container);
                this.Processor = new TransactionProcessor(this.Storage, this.ScriptRunner, this.Container);
            }

            [DebuggerStepThrough]
            public Task RunAsync()
            {
                var cancellation = new CancellationTokenSource();
                cancellation.CancelAfter(100);
                return this.Processor.RunAsync(cancellation.Token);
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
