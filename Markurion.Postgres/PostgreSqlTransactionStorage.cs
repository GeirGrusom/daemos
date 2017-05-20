using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using static NpgsqlTypes.NpgsqlDbType;
using System.Reflection;

namespace Markurion.Postgres
{
    public class PostgreSqlTransactionStorage : TransactionStorageBase
    {


        private const string SelectColumns = "id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error";

        private static readonly byte[] EmptyArray = new byte[0];
        private async Task<NpgsqlCommand> InsertTransactionCommandAsync()
        {
            NpgsqlCommand cmd = (await GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $@"
INSERT INTO markurion.transactions (id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error) 
VALUES (@id, @revision, @created, @expires, @expired, @payload, @script, @parentId, @parentRev, @state, @handler, @error)
RETURNING {SelectColumns};";
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("revision", Integer) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("created", Timestamp) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("expires", Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("expired", Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("payload", Jsonb) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("script", Text) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentId", Uuid) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentRev", Integer) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("state", Integer) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("handler", Varchar) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("error", Jsonb) { IsNullable = true });

            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionRevisionCommandAsync()
        {
            var cmd = (await GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM markurion.transactions WHERE id = @id AND revision = @revision";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Parameters.Add(new NpgsqlParameter("revision", Integer));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionCommandAsync()
        {
            var cmd = (await GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM markurion.transactions_head WHERE id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionChainCommandAsync()
        {
            var cmd = (await GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM markurion.transactions WHERE id = @id ORDER BY revision ASC";
            cmd.Parameters.Add(new NpgsqlParameter("id", Uuid));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectChildTransactionsCommandAsync()
        {
            var cmd = (await GetConnectionAsync()).CreateCommand();
            cmd.CommandText = "SELECT DISTINCT (id) FROM markurion.transactions_head WHERE parentId = @parentId AND state = ANY(@states)";
            cmd.Parameters.Add("parentId", Uuid);
            cmd.Parameters.Add("states", NpgsqlTypes.NpgsqlDbType.Array | Integer);
            cmd.Prepare();
            return cmd;
        }

        private readonly ConcurrentDictionary<Thread, NpgsqlConnection> _threadConnections = new ConcurrentDictionary<Thread, NpgsqlConnection>();

        private readonly ConcurrentDictionary<Thread, PostgreSqlQueryProvider> _queryProviders = new ConcurrentDictionary<Thread, PostgreSqlQueryProvider>();
        private async Task<NpgsqlConnection> GetConnectionAsync()
        {
            NpgsqlConnection conn = null;
            var result = _threadConnections.GetOrAdd(Thread.CurrentThread, thread => conn = CreateConnection());

            
            if (conn != null && conn != result)
            {
                conn.Dispose();
            }

            if (result.State != ConnectionState.Open)
            {
                await result.OpenAsync();
            }

            return result;
        }

        private NpgsqlConnection GetConnection()
        {
            NpgsqlConnection conn = null;
            var result = _threadConnections.GetOrAdd(Thread.CurrentThread, thread => conn = CreateConnection());


            if (conn != null && conn != result)
            {
                conn.Dispose();
            }

            if (result.State != ConnectionState.Open)
            {
                result.Open();
            }

            return result;
        }

        private async Task<PostgreSqlQueryProvider> GetQueryProviderAsync()
        {
            NpgsqlConnection conn = await GetConnectionAsync();
            return _queryProviders.GetOrAdd(Thread.CurrentThread, thread => new PostgreSqlQueryProvider(conn, this));
        }

        private readonly string connectionString;

        public PostgreSqlTransactionStorage(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public PostgreSqlTransactionStorage(string connectionString, ITimeService timeService)
            : base(timeService)
        {
            this.connectionString = connectionString;
        }


        private NpgsqlConnection CreateConnection()
        {
            var result = new NpgsqlConnection(connectionString);
            return result;
        }

        public override async Task InitializeAsync()
        {
            var conn = await GetConnectionAsync();

            using (var trans = conn.BeginTransaction())
            {
                var query = await QueryAsync();
                
                string initScript;
                using (var streamReader = new System.IO.StreamReader(GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Markurion.Postgres.Sql.v000_init.sql")))
                {
                    initScript = streamReader.ReadToEnd();
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = initScript;
                    await cmd.ExecuteNonQueryAsync();
                }
                await trans.CommitAsync();
            }
        }
        private static readonly byte[] EmptyState  = new byte[0];
        public override async Task<byte[]> GetTransactionStateAsync(Guid id, int revision)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT state FROM markurion.transaction_state where id = @Id and revision = @Revision";
                var idParam = cmd.CreateParameter();
                idParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;
                idParam.ParameterName = "Id";
                idParam.NpgsqlValue = id;
                var revisionParam = cmd.CreateParameter();
                revisionParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer;
                revisionParam.ParameterName = "Revision";
                revisionParam.NpgsqlValue = revision;
                cmd.Parameters.Add(idParam);
                cmd.Parameters.Add(revisionParam);

                cmd.Prepare();
                var result = (byte[])await cmd.ExecuteScalarAsync(CancellationToken.None);
                if (result == null)
                {
                    return EmptyState;
                }
                return result;
            }            
        }


        public override async Task OpenAsync()
        {
            var conn = await GetConnectionAsync();
        }

        private Transaction Map(DbDataReader reader)
        {
            TransactionData data = new TransactionData()
            {
                Id = reader.GetGuid(0),
                Revision = reader.GetInt32(1),
                Created = reader.GetDateTime(2),
                Expires = reader.IsDBNull(3) ? default(DateTime?) : reader.GetFieldValue<DateTime>(3),
                Expired = reader.IsDBNull(4) ? default(DateTime?) : reader.GetFieldValue<DateTime>(4),
                Payload = reader.IsDBNull(5) ? null : JsonConvert.DeserializeObject<ExpandoObject>(reader.GetString(5)),
                Script = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
            Guid? pid = reader.IsDBNull(7) ? default(Guid?) : reader.GetFieldValue<Guid>(7);
            if (pid != null)
            {
                data.Parent = new TransactionRevision(pid.Value, reader.GetInt32(8));
            }

            data.State = (TransactionState)reader.GetInt32(9);
            data.Handler = reader.IsDBNull(10) ? null : reader.GetString(10);
            data.Error = reader.IsDBNull(11) ? null : JsonConvert.DeserializeObject<ExpandoObject>(reader.GetString(11));
            return new Transaction(data, this);
        }

        public override async Task<Transaction> CommitTransactionDeltaAsync(Transaction original, Transaction next)
        {
            Transaction result;
            var conn = await GetConnectionAsync();
            using (var trans = conn.BeginTransaction())
            {
                int lastRev;
                using (var headRevCmd = conn.CreateCommand())
                {
                    var p = headRevCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.Value = original.Id;
                    headRevCmd.Parameters.Add(p);
                    headRevCmd.CommandText = "select revision from markurion.transactions_head where id = @id";
                    headRevCmd.Transaction = trans;
                    lastRev = (int)await headRevCmd.ExecuteScalarAsync();
                }

                if (next.Id != original.Id)
                    throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));

                if (next.Revision <= lastRev && next.Revision > 0)
                    throw new TransactionRevisionExistsException(next.Id, next.Revision);

                if (next.Revision <= 0 || next.Revision > lastRev + 1)
                    throw new ArgumentException("The specified revision number is not valid.", nameof(next));

                var parentId = next.Parent?.Id;
                var parentRevision = next.Parent?.Revision;

                var delta = new
                {
                    Id = original.Id,
                    Revision = next.Revision,
                    Expires = next.Expires,
                    Expired = next.Expired,
                    Created = TimeService.Now(),
                    Payload = new JsonContainer(JsonConvert.SerializeObject(next.Payload)),
                    Script = next.Script,
                    ParentId = parentId,
                    ParentRev = parentRevision,
                    State = (int)next.State,
                    Handler = next.Handler,
                    Error = new JsonContainer(JsonConvert.SerializeObject(next.Error)),
                };

                const string query = @"
update markurion.transactions set head = 'f' where id = @Id;
INSERT INTO markurion.transactions 
    (id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error) VALUES 
    (@Id, @Revision, @Created, @Expires, @Expired, @Payload, @Script, @ParentId, @ParentRev, @State, @Handler, @Error) RETURNING id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error;
";

                
                result = (await (conn).ExecuteReaderAsync(query, delta, Map, trans)).Single();
                await trans.CommitAsync();
            }
            OnTransactionCommitted(result);
            return result;
        }

        public override Transaction CommitTransactionDelta(Transaction original, Transaction next)
        {
            Transaction result;
            var conn = GetConnection();
            using (var trans = conn.BeginTransaction())
            {
                int lastRev;
                using (var headRevCmd = conn.CreateCommand())
                {
                    var p = headRevCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.Value = original.Id;
                    headRevCmd.Parameters.Add(p);
                    headRevCmd.CommandText = "select revision from markurion.transactions_head where id = @id";
                    headRevCmd.Transaction = trans;
                    lastRev = (int)headRevCmd.ExecuteScalar();
                }

                if (next.Id != original.Id)
                    throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));

                if (next.Revision <= lastRev && next.Revision > 0)
                    throw new TransactionRevisionExistsException(next.Id, next.Revision);

                if (next.Revision <= 0 || next.Revision > lastRev + 1)
                    throw new ArgumentException("The specified revision number is not valid.", nameof(next));

                var parentId = next.Parent?.Id;
                var parentRevision = next.Parent?.Revision;

                var delta = new
                {
                    Id = original.Id,
                    Revision = next.Revision,
                    Expires = next.Expires,
                    Expired = next.Expired,
                    Created = TimeService.Now(),
                    Payload = new JsonContainer(JsonConvert.SerializeObject(next.Payload)),
                    Script = next.Script,
                    ParentId = parentId,
                    ParentRev = parentRevision,
                    State = (int)next.State,
                    Handler = next.Handler,
                    Error = new JsonContainer(JsonConvert.SerializeObject(next.Error)),
                };

                const string query = @"
update markurion.transactions set head = 'f' where id = @Id;
INSERT INTO markurion.transactions 
    (id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error) VALUES 
    (@Id, @Revision, @Created, @Expires, @Expired, @Payload, @Script, @ParentId, @ParentRev, @State, @Handler, @Error) RETURNING id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, error;
";

                result = conn.ExecuteReader(query, delta, Map, trans).Single();
                trans.Commit();
            }
            OnTransactionCommitted(result);
            return result;
        }

        public override Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            return CreateTransaction(transaction, null);
        }

        public async Task<Transaction> CreateTransaction(Transaction transaction, NpgsqlTransaction sqlTransaction)
        {
            Transaction result;

            using (var cmd = await InsertTransactionCommandAsync())
            using (var trans = cmd.Connection.BeginTransaction())
            {
                using (var checkCmd = cmd.Connection.CreateCommand())
                {
                    checkCmd.Transaction = trans;
                    checkCmd.CommandText = "select exists(select 1 from markurion.transactions_head where id = @id)";
                    var p = checkCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;
                    p.Value = transaction.Id;
                    checkCmd.Parameters.Add(p);
                    checkCmd.Prepare();
                    var exists = (bool)await checkCmd.ExecuteScalarAsync();
                    if(exists)
                    {
                        await trans.RollbackAsync();
                        throw new TransactionExistsException(transaction.Id);
                    }
                }
                cmd.Transaction = sqlTransaction;
                cmd.Parameters["id"].Value = transaction.Id;
                cmd.Parameters["revision"].Value = 1;
                cmd.Parameters["created"].Value = TimeService.Now();
                cmd.Parameters["expires"].Value = transaction.Expires != null ? (object)transaction.Expires.Value : DBNull.Value;
                cmd.Parameters["expired"].Value = transaction.Expired != null ? (object)transaction.Expired.Value : DBNull.Value;
                cmd.Parameters["payload"].Value = transaction.Payload != null ? JsonConvert.SerializeObject(transaction.Payload) : (object)DBNull.Value;
                cmd.Parameters["script"].Value = transaction.Script != null ? (object)transaction.Script : DBNull.Value;
                cmd.Parameters["error"].Value = transaction.Error != null ? (object)JsonConvert.SerializeObject(transaction.Error) : DBNull.Value;
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

                using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                    {
                        throw new TransactionMissingException(transaction.Id);
                    }
                    result = Map(reader);
                }
                await trans.CommitAsync();
            }

            OnTransactionCommitted(result);
            return result;
            
        }

        public override async Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1)
        {
            if(revision == -1)
            {
                using (var select = await SelectTransactionCommandAsync()) {

                    select.Parameters["id"].Value = id;

                    using (var reader = (NpgsqlDataReader)await select.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                        {
                            throw new TransactionMissingException(id);
                        }
                        return Map(reader);

                    }
                }
            }

            using (var selectRevision = await SelectTransactionRevisionCommandAsync())
            {

                selectRevision.Parameters["id"].Value = id;
                selectRevision.Parameters["revision"].Value = revision;
                //selectRevision.Prepare();
                using (var revReader = (NpgsqlDataReader)await selectRevision.ExecuteReaderAsync())
                {
                    if (!revReader.Read())
                    {
                        throw new TransactionMissingException(id);
                    }
                    return Map(revReader);
                }
            }
        }
        
        public override async Task FreeTransactionAsync(Guid id)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                GuidHash(id, out int a, out int b);
                cmd.Parameters.Add("a", Integer).Value = a;
                cmd.Parameters.Add("b", Integer).Value = b;
                cmd.CommandText = "select pg_advisory_unlock(@a, @b)";
                await cmd.ExecuteNonQueryAsync();
            }

        }

        public override async Task<IEnumerable<Transaction>> GetChainAsync(Guid id)
        {
            using (var selectChain = await SelectTransactionChainCommandAsync())
            {
                selectChain.Parameters["id"].Value = id;
                using (var reader = (NpgsqlDataReader)await selectChain.ExecuteReaderAsync())
                {
                    var results = new List<Transaction>();
                    while(reader.Read())
                    {
                        results.Add(Map(reader));
                    }
                    return results;
                }
            }
            
        }

        public override async Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionState[] state)
        {
            using (var cmd = await SelectChildTransactionsCommandAsync())
            {
                cmd.Parameters["parentId"].Value = transaction;
                cmd.Parameters["states"].Value = state;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var results = new List<Transaction>();
                    while (reader.Read())
                    {
                        results.Add(Map(reader));
                    }
                    return results;
                }
            }
        }

        private async Task<DateTime?> GetNextExpiringTransactionTime()
        {
            string sql = "SELECT expires FROM markurion.transactions_head WHERE expires IS NOT NULL ORDER BY expires ASC LIMIT 1";
            using (var cmd = (await GetConnectionAsync()).CreateCommand())
            {
                cmd.CommandText = sql;
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return null;
                return (DateTime)result;
            }
        }

        private static void GuidHash(Guid id, out int a, out int b)
        {
            var bytes = id.ToByteArray();
            a = BitConverter.ToInt32(bytes, 0);
            b = BitConverter.ToInt32(bytes, 4);
        }

        protected override async Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel)
        {
            string sql = $"SELECT {SelectColumns} FROM markurion.transactions_head WHERE expires <= @now";
            var results = new List<Transaction>();

            using (var cmd = (await GetConnectionAsync()).CreateCommand())
            {
                cmd.CommandText = sql;
                var p = cmd.Parameters.Add(new NpgsqlParameter("now", Timestamp));
                p.Value = TimeService.Now();
                cmd.Prepare();
                bool failed = false;

                do
                {
                    try
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(Map(reader));
                            }
                        }
                    }
                    catch(InvalidOperationException)
                    {
                        failed = true;
                    }
                } while (failed);
            }
            SetNextExpiringTransactionTime(await GetNextExpiringTransactionTime());

            return results;

        }

        public override async Task<bool> IsTransactionLockedAsync(Guid id)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                GuidHash(id, out int a, out int b);
                cmd.Parameters.Add("id1", Integer).Value = a;
                cmd.Parameters.Add("id2", Integer).Value = b;


                cmd.CommandText = "select 1 from pg_locks where locktype = 'advisory' and classid = @id1 and objid = @id2";
                cmd.Prepare();

                return await cmd.ExecuteScalarAsync() != null;
            }

        }

        public override async Task LockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                GuidHash(id, out int a, out int b);
                cmd.Parameters.Add("a", Integer).Value = a;
                cmd.Parameters.Add("b", Integer).Value = b;
                if (timeout > 0)
                {
                    cmd.Parameters.Add("timeout", Integer).Value = timeout;
                    cmd.CommandText = "set local statement_timeout = @timeout; select pg_advisory_lock(@a, @b)";
                }
                else
                {
                    cmd.CommandText = "select pg_advisory_lock(@a, @b)";
                }
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public override async Task<IQueryable<Transaction>> QueryAsync()
        {
            return new PostgreSqlOrderedQuerableProvider<Transaction>(await GetQueryProviderAsync());
        }

        public override async Task<bool> TransactionExistsAsync(Guid id)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select exists(select 1 from markurion.transactions_head where id = @id)";
                var idPar = cmd.CreateParameter();
                idPar.Value = id;
                idPar.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;
                idPar.ParameterName = "id";
                cmd.Parameters.Add(idPar);
                cmd.Prepare();
                return (bool)await cmd.ExecuteScalarAsync();
            }
        }

        public override async Task<bool> TryLockTransactionAsync(Guid id, LockFlags flags = LockFlags.None, int timeout = -1)
        {
            var conn = await GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                GuidHash(id, out int a, out int b);
                cmd.Parameters.Add("a", Integer).Value = (long)a;
                cmd.Parameters.Add("b", Integer).Value = (long)b;

                cmd.CommandText = @"select pg_try_advisory_lock(@a, @b)";

                cmd.Prepare();
                
                return (bool)await cmd.ExecuteScalarAsync();
            }
        }

        public override void SaveTransactionState(Guid id, int revision, byte[] state)
        {
            var conn = GetConnection();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid);
                cmd.Parameters.Add("revision", NpgsqlTypes.NpgsqlDbType.Integer);
                cmd.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Bytea);
                cmd.Parameters["id"].Value = id;
                cmd.Parameters["revision"].Value = revision;
                cmd.Parameters["state"].Value = state;
                cmd.CommandText = "INSERT INTO markurion.transaction_state (id, revision, state) VALUES (@id, @revision, @state)";
                cmd.ExecuteNonQuery();
            }            
        }

        public override async Task SaveTransactionStateAsync(Guid id, int revision, byte[] state)
        {
            var conn = await GetConnectionAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid);
                cmd.Parameters.Add("revision", NpgsqlTypes.NpgsqlDbType.Integer);
                cmd.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Bytea);
                cmd.Parameters["id"].Value = id;
                cmd.Parameters["revision"].Value = revision;
                cmd.Parameters["state"].Value = state;
                cmd.CommandText = "INSERT INTO markurion.transaction_state (id, revision, state) VALUES (@id, @revision, @state)";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
