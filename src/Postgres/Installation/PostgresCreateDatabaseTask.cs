// <copyright file="PostgresCreateDatabaseTask.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
            Connection = connection;
        }

        public PostgresCreateDatabaseTask(string connectionString)
        {
            Connection = new NpgsqlConnection(connectionString);
            Connection.Open();
        }

        public async Task Install()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"create database daemos;";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task Rollback()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "drop database daemos;";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
