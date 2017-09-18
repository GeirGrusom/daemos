using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Daemos.Postgres;
using Xunit;

namespace Daemos.Tests
{

    public class PostgresDatabaseFixture
    {
        public string ConnectionString { get; }

        public string ContainerId { get; private set; }

        public string PostgresHostName { get; }

        public PostgresDatabaseFixture()
        {

            PostgresHostName = "localhost";
            string username = "transact_test";
            string password = "qwerty12345";

            //InitPostgres(username, password).Wait();

            ConnectionString = $@"User ID={username};Password={password};Host={PostgresHostName};Port=5432;Database=daemos;Pooling = true;";

            //var storage = new PostgreSqlTransactionStorage(ConnectionString);
            //storage.InitializeAsync().Wait();

        }
    }

    //[CollectionDefinition("Postgres collection")]
    public class PostgresDatabaseFixtureCollection : ICollectionFixture<PostgresDatabaseFixture>
    {

    }

    //[Collection("Postgres collection")]
    public class PostgreSqlStorageTests : TransactionStorageTests<PostgreSqlTransactionStorage>, IDisposable
    {
        private readonly PostgresDatabaseFixture _collection;
        public PostgreSqlStorageTests()
        {
            //_collection = collection;
            _collection = new PostgresDatabaseFixture();
        }

        protected override PostgreSqlTransactionStorage CreateStorage()
        {
            return new PostgreSqlTransactionStorage(_collection.ConnectionString);
        }

        protected override PostgreSqlTransactionStorage CreateStorage(ITimeService timeService)
        {
            return new PostgreSqlTransactionStorage(_collection.ConnectionString, timeService);
        }

        public void Dispose()
        {
            using (var conn = new Npgsql.NpgsqlConnection(_collection.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (SocketException)
                {
                    
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "delete from trans.transaction_state; delete from trans.transactions;";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}