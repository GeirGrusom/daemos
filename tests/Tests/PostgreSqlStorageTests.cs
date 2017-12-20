// <copyright file="PostgreSqlStorageTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.Postgres;
    using DockerDatabase;
    using Xunit;

    public class PostgresDatabaseFixture : Xunit.IAsyncLifetime
    {
        private DatabaseContainer container;

        public Npgsql.NpgsqlConnection Connection { get; private set; }

        public string ConnectionString => this.container.CreateConnectionString();

        public async Task InitializeAsync()
        {
            this.container = await DatabaseBuilder.CreateContainerAsync(DatabaseType.PostgreSql);
            this.Connection = await this.container.CreateConnectionAsync<Npgsql.NpgsqlConnection>();
            using (var stream = typeof(PostgreSqlTransactionStorage).Assembly.GetManifestResourceStream("Daemos.Postgres.Sql.v000_init.sql"))
            using (var reader = new System.IO.StreamReader(stream))
            {
                using (var cmd = this.Connection.CreateCommand())
                {
                    cmd.CommandText = await reader.ReadToEndAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DisposeAsync()
        {
            this.Connection.Dispose();
            await this.container.StopAsync();
        }
    }

    public class PostgreSqlStorageTests : TransactionStorageTests<PostgreSqlTransactionStorage>, IClassFixture<PostgresDatabaseFixture>
    {
        private readonly PostgresDatabaseFixture fixture;

        public PostgreSqlStorageTests(PostgresDatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        //public Task DisposeAsync() => this.fixture.DisposeAsync();

        //public Task InitializeAsync() => this.fixture.InitializeAsync();

        protected override PostgreSqlTransactionStorage CreateStorage()
        {
            return new PostgreSqlTransactionStorage(this.fixture.ConnectionString);
        }

        protected override PostgreSqlTransactionStorage CreateStorage(ITimeService timeService)
        {
            return new PostgreSqlTransactionStorage(this.fixture.ConnectionString, timeService);
        }
    }
}