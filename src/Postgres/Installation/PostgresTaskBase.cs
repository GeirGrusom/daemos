using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
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

        public void Install()
        {
            OnInstall();
            if (!Transaction.IsCompleted)
            {
                Transaction.Commit();
            }
        }

        public void Rollback()
        {
            OnRollback();
            Transaction.Rollback();
        }

        protected abstract void OnInstall();

        protected virtual void OnRollback()
        {
            
        }
    }
}
