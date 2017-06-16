using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
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

        public void Install()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"create database daemos;";
                cmd.ExecuteNonQuery();
            }
        }

        public void Rollback()
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "drop database daemos;";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
