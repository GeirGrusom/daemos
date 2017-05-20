using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Transact;

namespace UnitTests
{
    [TestFixture]
    public class TransactionFactoryTests
    {
        [Test]
        public async Task StartTransaction_Void_CreatesAndLocksTransaction()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();

            await st.Received().CreateTransaction(trans);
            await st.Received().LockTransaction(trans.Id);
        }

        [Test]
        public async Task StartTransaction_Void_SetUniqueId()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();

            Assert.That(trans.Id, Is.Not.EqualTo(default(Guid)));
        }

        [Test]
        public async Task StartTransaction_Guid_SetIdToGuid()
        {
            Guid id = new Guid(123, 456, 789, 1, 2, 3, 4, 5, 6,  7, 8);
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction(id);

            Assert.That(trans.Id, Is.EqualTo(id));
        }

        [Test]
        public async Task StartTransaction_Void_SetsCreatedTime()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();

            Assert.That(trans.Created, Is.Not.EqualTo(default(DateTime)));
        }
        [Test]
        public async Task StartTransaction_Void_SetsRevisionToZero()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();
            
            Assert.That(trans.Revision, Is.EqualTo(0));
        }

        [Test]
        public async Task StartTransaction_Void_SetsStateToInitialize()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();
            
            Assert.That(trans.State, Is.EqualTo(TransactionState.Initialized));
        }

        [Test]
        public async Task StartTransaction_Void_SetsStorageToCreator()
        {
            var st = Substitute.For<ITransactionStorage>();
            st.CreateTransaction(Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.Arg<Transaction>()));
            var fac = new TransactionFactory(st);
            var trans = await fac.StartTransaction();

            Assert.That(trans.Storage, Is.EqualTo(st));
        }

        [Test]
        public async Task ContinueTransaction_FetchesAndLocksTransaction()
        {
            var st = Substitute.For<ITransactionStorage>();

            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);
            st.FetchTransaction(Arg.Any<Guid>()).Returns(Task.FromResult(transaction));
            st.TryLockTransaction(Arg.Any<Guid>()).Returns(Task.FromResult(true));
            var fac = new TransactionFactory(st);

            await fac.ContinueTransaction(transaction.Id);

            await st.Received().FetchTransaction(transaction.Id);
            await st.Received().TryLockTransaction(transaction.Id);
        }
    }
}
