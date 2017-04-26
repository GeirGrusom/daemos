using BenchmarkDotNet.Attributes;
using Markurion.Postgres;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Markurion.Benchmark
{
    public class PostgresBenchmarks
    {

        private PostgreSqlTransactionStorage storage;

        private PostgreSqlTransactionStorage CreateStorage()
        {
            return new PostgreSqlTransactionStorage("User ID=transact_test;Password=qwerty12345;Host=localhost;Port=5432;Database=transact;Pooling = true;");
        }

        public PostgresBenchmarks()
        {
            storage = CreateStorage();
        }

        [Benchmark]
        public async Task CommitTransaction()
        {
            await storage.CreateTransactionAsync(new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null, storage));
        }

        [Cleanup]
        public void Cleanup()
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
}
