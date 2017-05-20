using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections;
using static NpgsqlTypes.NpgsqlDbType;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections.Concurrent;

namespace Transact.Postgres
{
    public class PostgreSqlTransactionStorage : TransactionStorageBase
    {
        private readonly NpgsqlConnection connection;
        private readonly PostgreSqlQueryProvider provider;

        private NpgsqlCommand InsertTransactionCommand;
        private NpgsqlCommand SelectTransactionRevisionCommand;
        private NpgsqlCommand SelectTransactionCommand;
        private NpgsqlCommand SelectTransactionChainCommand;
        private NpgsqlCommand SelectChildTransactionsCommand;

        private readonly SemaphoreSlim transactionLockSync = new SemaphoreSlim(1, 1);
        private readonly Dictionary<Guid, TransactionLock> transactionLocks;

        private void GenerateCommands()
        {
            const string SelectColumns = "\"Id\", \"Revision\", \"Created\", \"Expires\", \"Expired\", \"Payload\", \"Script\", \"ParentId\", \"ParentRevision\", \"State\", \"Handler\"";
            const string SelectChildColumns = "DISTINCT ON (\"Id\") \"Revision\", \"Created\", \"Expires\", \"Expired\", \"Payload\", \"Script\", \"ParentId\", \"ParentRevision\", \"State\", \"Handler\"";
            NpgsqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO tr.\"Transactions\" (\"Id\", \"Revision\", \"Expires\", \"Expired\", \"Payload\",\"Script\", \"ParentId\", \"ParentRevision\", \"State\", \"Handler\") VALUES (@id, @revision, @expires, @expired, @payload, @script, @parentId, @parentRev, @state, @handler) RETURNING \"Id\", \"Revision\", \"Created\", \"Expires\", \"Expired\", \"Payload\", \"Script\", \"ParentId\", \"ParentRevision\", \"State\", \"Handler\"";
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Parameters.Add(new NpgsqlParameter("revision", Integer));
            cmd.Parameters.Add(new NpgsqlParameter("expires", Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("expired", Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("payload", Jsonb) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("script", Text) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentId", Uuid) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentRev", Integer) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("state", Integer));
            cmd.Parameters.Add(new NpgsqlParameter("handler", Varchar) { IsNullable = true });

            cmd.Prepare();
            InsertTransactionCommand = cmd;

            cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM tr.\"Transactions\" WHERE \"Id\" = @id AND \"Revision\" = @revision";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Parameters.Add(new NpgsqlParameter("revision", Integer));
            cmd.Prepare();

            SelectTransactionRevisionCommand = cmd;

            cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM tr.\"TransactionHead\" WHERE \"Id\" = @id LIMIT 1";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Prepare();

            SelectTransactionCommand = cmd;

            cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM tr.\"Transactions\" WHERE \"Id\" = @id ORDER BY \"Revision\" ASC";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Prepare();

            SelectTransactionChainCommand = cmd;

            cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT DISTINCT (\"Id\") FROM tr.\"TransactionHead\" WHERE \"ParentId\" = @parentId AND \"State\" = ANY(@states)";
            cmd.Parameters.Add("parentId", Uuid);
            cmd.Parameters.Add("states", NpgsqlTypes.NpgsqlDbType.Array | Integer);
            cmd.Prepare();

            SelectChildTransactionsCommand = cmd;
        }

        public PostgreSqlTransactionStorage()
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                Database = "transaction",
                Host = "localhost",
                Username = "transact",
                Password = "abc123",
                               
            };
            transactionLocks = new Dictionary<Guid, TransactionLock>();
            connection = new NpgsqlConnection(builder);
            connection.Open();
            provider = new PostgreSqlQueryProvider(connection);
            GenerateCommands();
        }

        private IEnumerable<Transaction> Map(NpgsqlDataReader reader)
        {
            using (reader)
                while (reader.Read())
                {
                    TransactionData data = new TransactionData();
                    data.Id = reader.GetGuid(0);
                    data.Revision = reader.GetInt32(1);
                    data.Created = reader.GetDateTime(2);
                    data.Expires = reader.IsDBNull(3) ? default(DateTime?) : reader.GetFieldValue<DateTime>(3);
                    data.Expired = reader.IsDBNull(4) ? default(DateTime?) : reader.GetFieldValue<DateTime>(4);
                    data.Payload = reader.IsDBNull(5) ? null : JsonConvert.DeserializeObject<ExpandoObject>(reader.GetString(5));
                    data.Script = reader.IsDBNull(6) ? null : reader.GetString(6);
                    Guid? pid = reader.IsDBNull(7) ? default(Guid?) : reader.GetFieldValue<Guid>(7);
                    if (pid != null)
                    {
                        data.Parent = new TransactionRevision(pid.Value, reader.GetInt32(8));

                    }

                    data.State = (TransactionState)reader.GetInt32(9);
                    data.Handler = reader.IsDBNull(10) ? null : reader.GetString(10);
                    yield return new Transaction(data, this);
                }
        }

        public override Task<Transaction> CommitTransactionDelta(Transaction original, Transaction next)
        {
            if (next.Id != original.Id)
                throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));

            if (next.Revision != original.Revision + 1)
                throw new ArgumentException("The specified revision already exists.", nameof(next));

            var parentId = next.Parent != null ? next.Parent.Value.Id : default(Guid?);
            var parentRevision = next.Parent != null ? next.Parent.Value.Revision : default(int?);

            var delta = new
            {
                Id = original.Id,
                Revision = next.Revision,
                Expires = next.Expires,
                Expired = default(DateTime?),
                Payload = new JsonContainer(JsonConvert.SerializeObject(next.Payload)),
                Script = next.Script,
                ParentId = parentId,
                ParentRev = parentRevision,
                State = (int)next.State,
                Handler = next.Handler
            };

            Transaction result;
            using (var reader = connection.ExecuteReader(@"
begin transaction;
update tr.""Transactions"" set ""Head"" = 'f' where ""Id"" = @Id;
INSERT INTO tr.""Transactions"" 
    (""Id"", ""Revision"", ""Expires"", ""Expired"", ""Payload"",""Script"", ""ParentId"", ""ParentRevision"", ""State"", ""Handler"") VALUES 
    (@Id, @Revision, @Expires, @Expired, @Payload, @Script, @ParentId, @ParentRev, @State, @Handler) RETURNING ""Id"", ""Revision"", ""Created"", ""Expires"", ""Expired"", ""Payload"", ""Script"", ""ParentId"", ""ParentRevision"", ""State"", ""Handler"";
commit;
", delta))
            {
                
                result = Map(reader).Single();
            }
            OnTransactionCommitted(result);
            return Task.FromResult(result);
        }

        public override Task<Transaction> CreateTransaction(Transaction transaction)
        {
            return CreateTransaction(transaction, null);
        }

        public async Task<Transaction> CreateTransaction(Transaction transaction, NpgsqlTransaction sqlTransaction)
        {
            using(var cmd = InsertTransactionCommand.Clone())
            {
                cmd.Transaction = sqlTransaction;
                cmd.Parameters["id"].Value = transaction.Id;
                cmd.Parameters["revision"].Value = transaction.Revision;
                cmd.Parameters["expires"].Value = transaction.Expires != null ? (object)transaction.Expires.Value : DBNull.Value;
                cmd.Parameters["expired"].Value = transaction.Expired != null ? (object)transaction.Expired.Value : DBNull.Value;
                cmd.Parameters["payload"].Value = transaction.Payload != null ? JsonConvert.SerializeObject(transaction.Payload) : (object)DBNull.Value;
                cmd.Parameters["script"].Value = transaction.Script != null ? (object)transaction.Script : DBNull.Value;
                if (transaction.Parent != null)
                {
                    cmd.Parameters["parentId"].Value = transaction.Parent.Value.Id;
                    cmd.Parameters["parentRev"].Value = transaction.Parent.Value.Revision;
                }
                else
                {
                    cmd.Parameters["parentId"].Value = DBNull.Value;
                    cmd.Parameters["parentRev"].Value = DBNull.Value;
                }
                cmd.Parameters["state"].Value = (int)transaction.State;
                cmd.Parameters["handler"].Value = transaction.Handler != null ? (object)transaction.Handler : DBNull.Value;
                
                var reader = (NpgsqlDataReader) await cmd.ExecuteReaderAsync();

                var result = Map(reader).Single();

                OnTransactionCommitted(result);
                return result;
            }            
        }

        public override async Task<Transaction> FetchTransaction(Guid id, int revision = -1)
        {
            if(revision == -1)
            {
                SelectTransactionCommand.Parameters["id"].Value = id;
                var reader = (NpgsqlDataReader)await SelectTransactionCommand.ExecuteReaderAsync();
                return Map(reader).Single();
            }

            SelectTransactionRevisionCommand.Parameters["id"].Value = id;
            SelectTransactionRevisionCommand.Parameters["revision"].Value = revision;

            var revReader = (NpgsqlDataReader)await SelectTransactionRevisionCommand.ExecuteReaderAsync();
            return Map(revReader).Single();
        }
        
        private sealed class TransactionLock : IDisposable
        {
            private readonly SemaphoreSlim lockSemaphore;
            private readonly SemaphoreSlim waitSemaphore;

            public TransactionLock()
            {
                lockSemaphore = new SemaphoreSlim(1, 1);
                waitSemaphore = new SemaphoreSlim(1, 1);
            }

            public async Task<bool> FreeAsync()
            {
                lockSemaphore.Release();
                bool locked = await waitSemaphore.WaitAsync(0);
                if (locked)
                {
                    waitSemaphore.Release();
                    return true;
                }
                lockSemaphore.Release();
                return false;
            }

            public Task<bool> LockAsync(int timeout = Timeout.Infinite)
            {
                return lockSemaphore.WaitAsync(timeout);
            }

            public void Lock()
            {
                lockSemaphore.Wait();
            }

            public void Dispose()
            {
                lockSemaphore.Dispose();
                waitSemaphore.Dispose();
            }
        }

        public override async Task FreeTransaction(Guid id)
        {
            await transactionLockSync.WaitAsync();
            TransactionLock semaphore;
            if (transactionLocks.TryGetValue(id, out semaphore))
            {
                bool result = await semaphore.FreeAsync();
                if (!result)
                {
                    semaphore.Dispose();
                    transactionLocks.Remove(id);
                }
                transactionLockSync.Release();
                return;
            }
            transactionLockSync.Release();
            throw new SynchronizationLockException("Could not remove the lock.");
            
        }

        public override async Task<IEnumerable<Transaction>> GetChain(Guid id)
        {
            SelectTransactionChainCommand.Parameters["id"].Value = id;
            var reader = (NpgsqlDataReader) await SelectTransactionChainCommand.ExecuteReaderAsync();
            return Map(reader);
        }

        public override IEnumerable<Transaction> GetChildTransactions(Guid transaction, params TransactionState[] state)
        {
            using (var cmd = SelectChildTransactionsCommand.Clone())
            {
                cmd.Parameters["parentId"].Value = transaction;
                cmd.Parameters["states"].Value = state;
                return Map(cmd.ExecuteReader());
            }
        }

        private DateTime? GetNextExpiringTransactionTime()
        {
            string sql = "SELECT \"Expires\" FROM tr.\"TransactionHead\" WHERE \"Expired\" IS NULL AND \"Expires\" IS NOT NULL ORDER BY \"Expires\" ASC LIMIT 1";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return null;
                return (DateTime)result;
            }
        }

        protected override IEnumerable<Transaction> GetExpiringTransactionsInternal(DateTime now, CancellationToken cancel)
        {
            string sql = "SELECT * FROM tr.\"TransactionHead\" WHERE \"Expires\" <= @now";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                var p = cmd.Parameters.Add(new NpgsqlParameter("now", Timestamp));
                p.Value = now.ToUniversalTime();
                var reader = cmd.ExecuteReader();
                var results = Map(reader).ToArray();

                SetNextExpiringTransactionTime(GetNextExpiringTransactionTime());

                return results;
            }
        }

        public override async Task<bool> IsTransactionLocked(Guid id)
        {
            await transactionLockSync.WaitAsync();
            TransactionLock lc;
            if (transactionLocks.TryGetValue(id, out lc))
            {
                bool acquiredLock = await lc.LockAsync(0);
                await lc.FreeAsync();
                transactionLockSync.Release();
                return acquiredLock;
            }
            transactionLockSync.Release();
            return false;
        }

        public override async Task LockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            await transactionLockSync.WaitAsync();
            TransactionLock lc;
            if(transactionLocks.TryGetValue(id, out lc))
            {
                transactionLockSync.Release();
                await lc.LockAsync(timeout);
            }
            else
            {
                lc = new TransactionLock();
                transactionLocks.Add(id, lc);
                transactionLockSync.Release();
                await lc.LockAsync(timeout);
            }            
        }

        public override IQueryable<Transaction> Query()
        {
            return new PostgreSqlOrderedQuerableProvider<Transaction>(provider);
        }

        public override async Task<bool> TransactionExists(Guid id)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM tr.\"TransactionHead\" WHERE \"Id\" = @id";
                var idPar = cmd.CreateParameter();
                idPar.Value = id;
                idPar.ParameterName = "@id";
                var result = (long)await cmd.ExecuteScalarAsync();
                return result > 0;
            }
        }

        public override async Task<bool> TryLockTransaction(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            await transactionLockSync.WaitAsync();
            TransactionLock lc;
            if(transactionLocks.TryGetValue(id, out lc))
            {
                transactionLockSync.Release();
                var result = await lc.LockAsync(timeout);
                if(result)
                {
                    return result;
                }
            } else
            {
                lc = new TransactionLock();
                transactionLocks.Add(id, lc);
                transactionLockSync.Release();
                return await lc.LockAsync(timeout);
            }
            return false;
        }
    }
}
