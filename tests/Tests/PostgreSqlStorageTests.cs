using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Daemos.Postgres;
using Docker.DotNet;
using Docker.DotNet.Models;
using Xunit;

namespace Daemos.Tests
{

    public class PostgresDatabaseFixture : IDisposable
    {
        public string ConnectionString { get; }

        public string ContainerId { get; private set; }

        public string PostgresHostName { get; }

        public PostgresDatabaseFixture()
        {

            PostgresHostName = "localhost";
            string username = "transact";
            string password = "qwerty12345";

            //InitPostgres(username, password).Wait();

            ConnectionString = $@"User ID={username};Password={password};Host={PostgresHostName};Port=5432;Database=daemos;Pooling = true;";

            //var storage = new PostgreSqlTransactionStorage(ConnectionString);
            //storage.InitializeAsync().Wait();

        }

        private DockerClient CreateDockerClient() => new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
        public async Task InitPostgres(string username, string password)
        {
            var client = CreateDockerClient();

            var postgresImageResult = await client.Images.PullImageAsync(new ImagesPullParameters { Parent = "postgres", Tag = "9.6.2" }, null);

            var buffer = new byte[65536];
            var builder = new System.Text.StringBuilder();
            int readBytes = 0;
            do
            {
                readBytes = await postgresImageResult.ReadAsync(buffer, 0, buffer.Length);
                builder.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, readBytes));
            } while (readBytes > 0);



            var createParameters = new CreateContainerParameters
            {
                Env = new List<string>
                {
                    $"POSTGRES_USER={username}",
                    $"POSTGRES_PASSWORD={password}"
                },
                Hostname = PostgresHostName,
                Image = "postgres:9.6.2",
                HostConfig = new HostConfig
                {
                }
            };

            var createdContainer = await client.Containers.CreateContainerAsync(createParameters);

            var startedContainer = await client.Containers.StartContainerAsync(createdContainer.ID, new ContainerStartParameters());

            this.ContainerId = createdContainer.ID;
        }

        public void Dispose()
        {
            var client = CreateDockerClient();

            client.Containers.StopContainerAsync(ContainerId, new ContainerStopParameters { WaitBeforeKillSeconds = 30 }, CancellationToken.None).Wait();
            client.Containers.RemoveContainerAsync(ContainerId, new ContainerRemoveParameters());
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