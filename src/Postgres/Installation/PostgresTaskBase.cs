// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

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
            this.Connection = connection;
            this.Connection.Open();
            this.Transaction = this.Connection.BeginTransaction(IsolationLevel.Serializable);
        }

        protected PostgresTaskBase(string connectionString)
            : this(new NpgsqlConnection(connectionString))
        {
        }

        public void Dispose()
        {
            this.Transaction.Dispose();
            this.Connection.Dispose();
        }

        public async Task Install()
        {
            await this.OnInstall();
            if (!this.Transaction.IsCompleted)
            {
                await this.Transaction.CommitAsync();
            }
        }

        public async Task Rollback()
        {
            await this.OnRollback();
            await this.Transaction.RollbackAsync();
        }

        protected abstract Task OnInstall();

        protected virtual Task OnRollback()
        {
            return Task.CompletedTask;
        }
    }
}
