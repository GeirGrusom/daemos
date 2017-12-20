// <copyright file="PostgresBenchmarks.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Benchmark
{
    using System;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Daemos.Postgres;

    public class PostgresBenchmarks
    {
        private PostgreSqlTransactionStorage storage;

        public PostgresBenchmarks()
        {
            this.storage = this.CreateStorage();
        }

        [Benchmark]
        public async Task CommitTransaction()
        {
            await this.storage.CreateTransactionAsync(new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionStatus.Initialized, null, null, this.storage));
        }

        [GlobalCleanup]
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

        private PostgreSqlTransactionStorage CreateStorage()
        {
            return new PostgreSqlTransactionStorage("User ID=transact_test;Password=qwerty12345;Host=localhost;Port=5432;Database=transact;Pooling = true;");
        }
    }
}
