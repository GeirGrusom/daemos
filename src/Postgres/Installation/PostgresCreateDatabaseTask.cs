// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Daemos.Installation;
using Npgsql;

namespace Daemos.Postgres.Installation
{
    public sealed class PostgresCreateDatabaseTask : ITask
    {
        public NpgsqlConnection Connection { get; }

        public PostgresCreateDatabaseTask(NpgsqlConnection connection)
        {
            this.Connection = connection;
        }

        public PostgresCreateDatabaseTask(string connectionString)
        {
            this.Connection = new NpgsqlConnection(connectionString);
            this.Connection.Open();
        }

        public async Task Install()
        {
            using (var cmd = this.Connection.CreateCommand())
            {
                cmd.CommandText = @"create database daemos;";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task Rollback()
        {
            using (var cmd = this.Connection.CreateCommand())
            {
                cmd.CommandText = "drop database daemos;";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
