using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Transact;

namespace UnitTests
{
    [TestFixture]
    public class TransactionProcessorTests
    {
        [Test]
        public async Task RunAsync_ExpiresTransaction()
        {
            var cancel = new CancellationTokenSource();
            var storage = Substitute.For<ITransactionStorage>();
            var scriptRunner = Substitute.For<IScriptRunner>();

            var processor = new TransactionProcessor(storage, scriptRunner);

            await processor.RunAsync(cancel.Token);
        }
    }
}
