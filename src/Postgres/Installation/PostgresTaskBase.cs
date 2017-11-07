// <copyright file="PostgresTaskBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Daemos.Installation;
using Npgsql;

namespace Daemos.Postgres.Installation
{
    public abstract class PostgresTaskBase : ITask, IDisposable
    {
        public NpgsqlConnection Connection { get; }

        public NpgsqlTransaction Transaction { get; }

        protected PostgresTaskBase(NpgsqlConnection connection)
        {
            Connection = connection;
            Connection.Open();
            Transaction = Connection.BeginTransaction(IsolationLevel.Serializable);
        }

        protected PostgresTaskBase(string connectionString)
            : this(new NpgsqlConnection(connectionString))
        {
        }

        public void Dispose()
        {
            Transaction.Dispose();
            Connection.Dispose();
        }

        public async Task Install()
        {
            await OnInstall();
            if (!Transaction.IsCompleted)
            {
                await Transaction.CommitAsync();
            }
        }

        public async Task Rollback()
        {
            await OnRollback();
            await Transaction.RollbackAsync();
        }

        protected abstract Task OnInstall();

        protected virtual Task OnRollback()
        {
            return Task.CompletedTask;
        }
    }
}
