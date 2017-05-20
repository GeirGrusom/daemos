using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Irony.Parsing;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Transact;
using Transact.Api;

namespace UnitTests
{
    [TestFixture]
    public class MemoryStorageTests
    {

        [Test]
        public async Task CreateTransaction_ReturnsNewTransaction()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            var result = await st.CreateTransaction(transaction);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task FetchTransaction_ReturnsCreatedTransaction()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);
            var result = await st.FetchTransaction(transaction.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(transaction.Id));
        }

        [Test]
        public async Task CommitTransactionDelta_AppendsToTheChain()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);
            await
                st.CommitTransactionDelta(transaction,
                    new Transaction(transaction.Id, 1, DateTime.UtcNow, null, null, null, null,
                        TransactionState.Initialized, null, st));

            var result = await st.FetchTransaction(transaction.Id);

            Assert.That(result.Revision, Is.EqualTo(1));
        }

        [Test]
        public async Task CommitTransactionDelta_WrongRevision_ThrowsArgumentException()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);
            Assert.That(() => st.CommitTransactionDelta(transaction,
                    new Transaction(transaction.Id, 0, DateTime.UtcNow, null, null, null, null,
                        TransactionState.Initialized, null, st) ).Wait(), Throws.ArgumentException);
        }

        [Test]
        public async Task CommitTransactionDelta_WrongId_ThrowsArgumentException()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);
            Assert.That(() => st.CommitTransactionDelta(transaction,
                    new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null,
                        TransactionState.Initialized, null, st)).Wait(), Throws.ArgumentException);
        }

        [Test]
        public async Task LockTransaction_TryLock_Fails()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            await st.LockTransaction(transaction.Id, LockFlags.None, 0);
            try
            {
                Assert.That(await st.TryLockTransaction(transaction.Id, LockFlags.None, 0), Is.False);
            }
            finally
            {
                await st.FreeTransaction(transaction.Id);
            }
        }

        [Test]
        public async Task LockTransaction_LockTransaction_ThrowsException()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            await st.LockTransaction(transaction.Id, LockFlags.None, 0);
            try
            {
                Assert.That(async () => await st.LockTransaction(transaction.Id, LockFlags.None, 0), Throws.TypeOf<TimeoutException>());
            }
            finally
            {
                await st.FreeTransaction(transaction.Id);
            }
        }

        [Test]
        public async Task IsTransactionLocked_ReturnsTrue()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            await st.LockTransaction(transaction.Id, LockFlags.None, 0);

            Assert.That(await st.IsTransactionLocked(transaction.Id), Is.True);
            await st.FreeTransaction(transaction.Id);
        }

        [Test]
        public async Task IsTransactionLocked_ReturnsFalse()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            Assert.That(await st.IsTransactionLocked(transaction.Id), Is.False);
        }

        [Test]
        public async Task FreeTransaction_IsTransactionLocked_ReturnsFalse()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            await st.LockTransaction(transaction.Id, LockFlags.None, 0);
            await st.FreeTransaction(transaction.Id);

            Assert.That(await st.IsTransactionLocked(transaction.Id), Is.False);
        }

        public static object[] DateSource()
        {
            return new object[]
            {
                new object[]
                {
                    new DateTime(2000, 1, 15),
                    new []
                    {
                        new DateTime(2000, 1, 2),
                    },
                    new []
                    {
                        new DateTime(2000, 1, 2)
                    }
                },

                new object[]
                {
                    new DateTime(2000, 1, 15),
                    new []
                    {
                        new DateTime(2000, 2, 1),
                    },
                    new DateTime[]
                    {
                        
                    }
                },
                new object[]
                {
                    new DateTime(2000, 1, 15),
                    new []
                    {
                        new DateTime(2000, 1, 2),
                        new DateTime(2000, 2, 15),
                    },
                    new []
                    {
                        new DateTime(2000, 1, 2)
                    }
                },
                new object[]
                {
                    new DateTime(2000, 1, 5),
                    new []
                    {
                        new DateTime(2000, 1, 1),
                        new DateTime(2000, 1, 2),
                        new DateTime(2000, 1, 3),
                        new DateTime(2000, 1, 4),
                        new DateTime(2000, 1, 5),
                        new DateTime(2000, 1, 6),
                        new DateTime(2000, 1, 7),
                        new DateTime(2000, 1, 8),
                    },
                    new []
                    {
                        new DateTime(2000, 1, 5),
                        new DateTime(2000, 1, 4),
                        new DateTime(2000, 1, 3),
                        new DateTime(2000, 1, 2),
                        new DateTime(2000, 1, 1),
                    }
                },

                new object[]
                {
                    new DateTime(2001, 1, 5),
                    new []
                    {
                        new DateTime(2001, 1, 2),
                        new DateTime(2001, 1, 3),
                        new DateTime(2001, 1, 4),
                        new DateTime(2001, 1, 5),
                        new DateTime(2001, 1, 6),
                        new DateTime(2001, 1, 7),
                        new DateTime(2001, 1, 8),
                    },
                    new []
                    {
                        new DateTime(2001, 1, 5),
                        new DateTime(2001, 1, 4),
                        new DateTime(2001, 1, 3),
                        new DateTime(2001, 1, 2),
                    }
                }
            };
        }

        [TestCaseSource(nameof(DateSource))]
        public async Task GetExpiringTransactions_ReturnsExpiringTransaction(DateTime now, DateTime[] transactionTimes, DateTime[] expectedTimes)
        {
            var st = new MemoryStorage();

            foreach (var date in transactionTimes)
            {
                var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, date.ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st);
                await st.CreateTransaction(transaction);
            }
            

            var transactions = st.GetExpiringTransactions(now.ToUniversalTime(), CancellationToken.None).ToArray();

            Assert.That(transactions.Select(t => t.Expires.Value), Is.EquivalentTo(expectedTimes.Select(x => x.ToUniversalTime())));

        }

        [Test]
        public async Task GetExpiringTransactions_GetsTheCorrectNumberOfTransactions()
        {
            var st = new MemoryStorage();
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 1).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 2).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 3).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 4).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 5).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 6).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 7).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));
            await st.CreateTransaction(new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 8).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st));


            var transactions = st.GetExpiringTransactions(new DateTime(2001, 1, 5, 0, 0, 0).ToUniversalTime(), CancellationToken.None).ToArray();
            var dates = transactions.Select(x => x.Expires.Value).ToArray();

            var expectedDates = new[]
            {
                new DateTime(2001, 1, 5).ToUniversalTime(),
                new DateTime(2001, 1, 4).ToUniversalTime(),
                new DateTime(2001, 1, 3).ToUniversalTime(),
                new DateTime(2001, 1, 2).ToUniversalTime(),
                new DateTime(2001, 1, 1).ToUniversalTime(),
            };

            Assert.That(dates, Is.EquivalentTo(expectedDates));
            

        }

        [Test]
        public async Task GetExpiringTransactions_NoExpiringTransactions_ReturnsEmptySet()
        {
            var st = new MemoryStorage();
            var transaction1 = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 1).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st);
            var transaction2 = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2000, 1, 1, 12, 0, 1).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction1);
            await st.CreateTransaction(transaction2);

            var transactions = st.GetExpiringTransactions(new DateTime(1999, 1, 1, 0, 0, 0).ToUniversalTime(), CancellationToken.None).ToArray();

            Assert.That(transactions, Is.Empty);
        }

        [Test]
        public async Task TransactionExists_ReturnsTrue()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 1).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            Assert.That(await st.TransactionExists(transaction.Id), Is.True);
        }

        [Test]
        public async Task TransactionExists_ReturnsFalse()
        {
            var st = new MemoryStorage();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, new DateTime(2001, 1, 1).ToUniversalTime(), null, null, null, TransactionState.Initialized, null, st);

            await st.CreateTransaction(transaction);

            Assert.That(await st.TransactionExists(Guid.NewGuid()), Is.False);
        }

    }
}
