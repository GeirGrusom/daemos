using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using static Xunit.Assert;
using Markurion.Postgres;

namespace Markurion.Tests
{

    public class MemoryStorageTests : TransactionStorageTests<MemoryStorage>
    {
        protected override MemoryStorage CreateStorage()
        {
            return new MemoryStorage();
        }

        protected override MemoryStorage CreateStorage(ITimeService timeService)
        {
            return new MemoryStorage(timeService);
        }

    }

    public class PostgreSqlStorageTests : TransactionStorageTests<Postgres.PostgreSqlTransactionStorage>, IDisposable
    {
        protected override PostgreSqlTransactionStorage CreateStorage()
        {
            return new PostgreSqlTransactionStorage("User ID=transact_test;Password=qwerty12345;Host=localhost;Port=5432;Database=transact;Pooling = true;");
        }

        protected override PostgreSqlTransactionStorage CreateStorage(ITimeService timeService)
        {
            return new PostgreSqlTransactionStorage("User ID=transact_test;Password=qwerty12345;Host=localhost;Port=5432;Database=transact;Pooling = true;", timeService);
        }

        public void Dispose()
        {
            using (var conn = new Npgsql.NpgsqlConnection("User ID=transact_test;Password=qwerty12345;Host=localhost;Port=5432;Database=transact;Pooling = true;"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "delete from tr.transactions;";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public abstract class TransactionStorageTests<T> where T : ITransactionStorage
    {
        protected abstract T CreateStorage();
        protected abstract T CreateStorage(ITimeService timeService);

        [Fact]
        public async Task CreateTransaction_ReturnsTransactionWithSameId()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage);

            // Act
            var committedTransaction = await storage.CreateTransactionAsync(transaction);

            // Assert
            Equal(transaction.Id, committedTransaction.Id);
        }

        [Fact]
        public async Task CreateTransaction_ReturnsTransactionWithSameState()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage);

            // Act
            var committedTransaction = await storage.CreateTransactionAsync(transaction);

            // Assert
            Equal(transaction.State, committedTransaction.State);
        }

        [Fact]
        public async Task CreateTransaction_TransactionAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage);
            var committedTransaction = await storage.CreateTransactionAsync(transaction);

            // Act
            await ThrowsAsync<TransactionExistsException>(() => storage.CreateTransactionAsync(transaction));

            // Assert
            Equal(transaction.State, committedTransaction.State);
        }


        [Theory]
        [InlineData(TransactionState.Initialized)]
        [InlineData(TransactionState.Authorized)]
        [InlineData(TransactionState.Completed)]
        [InlineData(TransactionState.Cancelled)]
        [InlineData(TransactionState.Failed)]
        public async Task CreateTransaction_ReturnsTransactionWithSameState_StateSpecified(TransactionState state)
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage).With((ref TransactionData t) => t.State = state);

            // Act
            var committedTransaction = (await storage.CreateTransactionAsync(transaction));

            // Assert
            Equal(state, committedTransaction.State);
        }

        [Fact]
        public async Task CreateTransaction_RevisionIsIgnored()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage).With((ref TransactionData t) => { t.Revision = 2; });

            // Act
            var committedTransaction = await storage.CreateTransactionAsync(transaction);

            // Assert
            Equal(1, committedTransaction.Revision);
        }

        [Fact]
        public async Task CreateTransaction_InvokedTransactionCommitted()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage).With((ref TransactionData t) => { t.Revision = 2; });

            // Act
            var ev = await RaisesAsync<TransactionCommittedEventArgs>(v => storage.TransactionCommitted += v, v => storage.TransactionCommitted -= v, () => storage.CreateTransactionAsync(transaction));

            // Assert
            Equal(transaction.Id, ev.Arguments.Transaction.Id);
        }

        [Fact]
        public async Task FetchTransaction_RetrievesCommittedTransaction()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = TransactionFactory.CreateNew(storage);
            var committedTransaction = await storage.CreateTransactionAsync(transaction);

            // Act
            var fetchedTransaction = await storage.FetchTransactionAsync(transaction.Id);

            // Assert
            Equal(committedTransaction, fetchedTransaction);
        }

        [Fact]
        public async Task FetchTransaction_DoesNotExist_ThrowsTransactionMissingException()
        {
            // Arrange
            var storage = CreateStorage();
            var transactionId = Guid.NewGuid();

            // Act
            var ex = await ThrowsAsync<TransactionMissingException>(() => storage.FetchTransactionAsync(transactionId));

            // Assert
            Equal(transactionId, ex.TransactionId);
        }

        [Fact]
        public async Task CommitTransactionDelta_CreatesDelta()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = await storage.CreateTransactionAsync(TransactionFactory.CreateNew(storage));
            var delta = transaction.With((ref TransactionData t) => { t.State = TransactionState.Authorized; t.Revision = 2; });

            // Act
            var result = await storage.CommitTransactionDeltaAsync(transaction, delta);

            // Assert
            Equal(2, result.Revision);
            Equal(TransactionState.Authorized, result.State);
        }

   
        [Fact]
        public async Task CommitTransactionDelta_RevisionExists_ThrowsTransactionRevisionExistsException()
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = await storage.CreateTransactionAsync(TransactionFactory.CreateNew(storage));

            // Act
            var ex = await ThrowsAsync<TransactionRevisionExistsException>(() => storage.CommitTransactionDeltaAsync(transaction, transaction));

            // Assert
            Equal(transaction.Id, ex.TransactionId);
            Equal(transaction.Revision, ex.Revision);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task CommitTransactionDelta_RevisionInvalid_ThrowsArgumentException(int revision)
        {
            // Arrange
            var storage = CreateStorage();
            var transaction = await storage.CreateTransactionAsync(TransactionFactory.CreateNew(storage));
            var delta = transaction.With((ref TransactionData t) => { t.Revision = revision; });

            // Act
            var ex = await ThrowsAsync<ArgumentException>(() => storage.CommitTransactionDeltaAsync(transaction, delta));

            // Assert
            Equal("next", ex.ParamName);
        }


        [Fact]
        public async Task GetExpiringTransactions_RetrivesExpiredTransacions()
        {
            // Arrange
            var timeService = Substitute.For<ITimeService>();
            timeService.Now().Returns(new DateTime(1999, 05, 15));
            var storage = CreateStorage(timeService);
            var trans = TransactionFactory.CreateNew(storage).With((ref TransactionData t) => { t.Expires = new DateTime(1999, 05, 14); });
            trans = await storage.CreateTransactionAsync(trans);

            // Act
            var expiringTransactions = await storage.GetExpiringTransactionsAsync(CancellationToken.None);

            // Assert
            Equal(trans, expiringTransactions[0]);
        }

        [Fact]
        public async Task GetExpiringTransactions_NoPendingTransactions_ReturnsEmptyList()
        {
            // Arrange
            var timeService = Substitute.For<ITimeService>();
            timeService.Now().Returns(new DateTime(1999, 05, 15));
            var storage = CreateStorage(timeService);

            // Act
            var expiringTransactions = await storage.GetExpiringTransactionsAsync(CancellationToken.None);

            // Assert
            Empty(expiringTransactions);
        }

        [Fact]
        public async Task TransactionExists_ReturnsTrue()
        {
            // Arrange
            var storage = CreateStorage();
            var trans = TransactionFactory.CreateNew(storage);
            await storage.CreateTransactionAsync(trans);

            // Act
            var result = await storage.TransactionExistsAsync(trans.Id);

            // Assert
            True(result);
        }

        [Fact]
        public async Task TransactionExists_ReturnsFalse()
        {
            // Arrange
            var storage = CreateStorage();

            // Act
            var result = await storage.TransactionExistsAsync(Guid.NewGuid());

            // Assert
            False(result);
        }

        [Fact]
        public async Task GetChain_ReturnsChain()
        {
            // Arrange
            var storage = CreateStorage();
            var tr1 = TransactionFactory.CreateNew(storage);
            var tr2 = tr1.With((ref TransactionData tr) => { tr.Revision = 2; });
            tr1 = await storage.CreateTransactionAsync(tr1);
            tr2 = await storage.CommitTransactionDeltaAsync(tr1, tr2);

            // Act
            var chain = (await storage.GetChainAsync(tr1.Id)).ToArray();

            // Assert
            Equal(new[] { tr1, tr2 }, chain);
        }

        public async Task CreateTransaction_UsesTimeService_AsCreatedTime()
        {
            // Arrange
            var timeService = Substitute.For<ITimeService>();
            var created = new DateTime(1999, 01, 05, 12, 0, 0, DateTimeKind.Utc);
            timeService.Now().Returns(created);
            var storage = CreateStorage(timeService);

            // Act
            var tr = await storage.CreateTransactionAsync(TransactionFactory.CreateNew(storage));

            // Assert
            Equal(created, tr.Created);
        }

        public async Task CommitTransactionDelta_UsesTimeService_AsCreatedTime()
        {
            // Arrange
            var timeService = Substitute.For<ITimeService>();
            var created = new DateTime(1999, 01, 05, 12, 0, 0, DateTimeKind.Utc);
            var updated = new DateTime(2000, 01, 05, 12, 0, 0, DateTimeKind.Utc);
            timeService.Now().Returns(created, updated);
            var storage = CreateStorage(timeService);
            var tr = await storage.CreateTransactionAsync(TransactionFactory.CreateNew(storage));
            var trNext = tr.Data;
            trNext.Revision += 1;

            // Act
            var result = await storage.CommitTransactionDeltaAsync(tr, new Transaction(trNext, storage));

            // Assert
            Equal(updated, result.Created);
        }


        [Fact]
        public async Task Query_GetById_ReturnsTransaction()
        {
            // Arrange
            var storage = CreateStorage();
            var tr = TransactionFactory.CreateNew(storage);
            var guid = tr.Id;
            tr = await storage.CreateTransactionAsync(tr);

            // Act
            var result = (await storage.QueryAsync()).Where(x => x.Id == guid).ToArray();

            // Assert
            Equal(tr, result.Single());
        }

        [Fact]
        public async Task Query_GetByCreated_ReturnsTransaction()
        {
            // Arrange
            var created = new DateTime(1999, 01, 05, 12, 0, 0, DateTimeKind.Utc);
            var timeService = Substitute.For<ITimeService>();
            timeService.Now().Returns(created);
            var storage = CreateStorage(timeService);
            var tr = TransactionFactory.CreateNew(storage);
            var guid = tr.Id;
            tr = await storage.CreateTransactionAsync(tr);

            // Act
            var result = (await storage.QueryAsync()).Where(x => x.Created == created).ToArray();

            // Assert
            Equal(tr, result.Single());
        }
    }
}
