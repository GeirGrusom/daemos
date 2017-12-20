// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Dynamic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Npgsql;
    using NpgsqlTypes;

    public class PostgreSqlTransactionStorage : TransactionStorageBase
    {
        private const string Schema = "trans";

        private const string SelectColumns = "id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error";

        private static readonly byte[] EmptyArray = new byte[0];

        private readonly ConcurrentDictionary<Thread, NpgsqlConnection> threadConnections = new ConcurrentDictionary<Thread, NpgsqlConnection>();

        private readonly ConcurrentDictionary<Thread, PostgreSqlQueryProvider> queryProviders = new ConcurrentDictionary<Thread, PostgreSqlQueryProvider>();

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

        private static void GuidHash(Guid id, out int a, out int b)
        {
            var bytes = id.ToByteArray();
            a = BitConverter.ToInt32(bytes, 0);
            b = BitConverter.ToInt32(bytes, 4);
        }

        public override async Task InitializeAsync()
        {
            var conn = await this.GetConnectionAsync();

            using (var trans = conn.BeginTransaction())
            {
                var query = await this.QueryAsync();

                string initScript;
                using (var streamReader = new System.IO.StreamReader(this.GetType().GetTypeInfo().Assembly.GetManifestResourceStream("Daemos.Postgres.Sql.v000_init.sql")))
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

        public override async Task<byte[]> GetTransactionStateAsync(Guid id, int revision)
        {
            var conn = await this.GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT state FROM {Schema}.transaction_state where id = @Id and revision = @Revision";
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
                    return EmptyArray;
                }

                return result;
            }
        }

        public override async Task OpenAsync()
        {
            var conn = await this.GetConnectionAsync();
        }

        public override async Task<Transaction> CommitTransactionDeltaAsync(Transaction original, Transaction next)
        {
            Transaction result;
            var conn = await this.GetConnectionAsync();
            using (var trans = conn.BeginTransaction())
            {
                int lastRev;
                using (var headRevCmd = conn.CreateCommand())
                {
                    var p = headRevCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.Value = original.Id;
                    headRevCmd.Parameters.Add(p);
                    headRevCmd.CommandText = $"select revision from {Schema}.transactions_head where id = @id";
                    headRevCmd.Transaction = trans;
                    lastRev = (int)await headRevCmd.ExecuteScalarAsync();
                }

                if (next.Id != original.Id)
                {
                    throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));
                }

                if (next.Revision <= lastRev && next.Revision > 0)
                {
                    throw new TransactionRevisionExistsException(next.Id, next.Revision);
                }

                if (next.Revision <= 0 || next.Revision > lastRev + 1)
                {
                    throw new ArgumentException("The specified revision number is not valid.", nameof(next));
                }

                var parentId = next.Parent?.Id;
                var parentRevision = next.Parent?.Revision;

                var delta = new
                {
                    Id = original.Id,
                    Revision = next.Revision,
                    Expires = next.Expires,
                    Expired = next.Expired,
                    Created = this.TimeService.Now(),
                    Payload = new JsonContainer(JsonConvert.SerializeObject(next.Payload)),
                    Script = next.Script,
                    ParentId = parentId,
                    ParentRev = parentRevision,
                    Status = (int)next.Status,
                    Error = new JsonContainer(JsonConvert.SerializeObject(next.Error)),
                };

                string query = $@"
update {Schema}.transactions set head = 'f' where id = @Id;
INSERT INTO {Schema}.transactions 
    (id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error) VALUES 
    (@Id, @Revision, @Created, @Expires, @Expired, @Payload, @Script, @ParentId, @ParentRev, @Status, @Error) RETURNING id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error;
";

                result = (await conn.ExecuteReaderAsync(query, delta, this.Map, trans)).Single();
                await trans.CommitAsync();
            }

            this.OnTransactionCommitted(result);
            return result;
        }

        public override Transaction CommitTransactionDelta(Transaction original, Transaction next)
        {
            Transaction result;
            var conn = this.GetConnection();
            using (var trans = conn.BeginTransaction())
            {
                int lastRev;
                using (var headRevCmd = conn.CreateCommand())
                {
                    var p = headRevCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.Value = original.Id;
                    headRevCmd.Parameters.Add(p);
                    headRevCmd.CommandText = $"select revision from {Schema}.transactions_head where id = @id";
                    headRevCmd.Transaction = trans;
                    lastRev = (int)headRevCmd.ExecuteScalar();
                }

                if (next.Id != original.Id)
                {
                    throw new ArgumentException("The new transaction has a different id from the chain.", nameof(next));
                }

                if (next.Revision <= lastRev && next.Revision > 0)
                {
                    throw new TransactionRevisionExistsException(next.Id, next.Revision);
                }

                if (next.Revision <= 0 || next.Revision > lastRev + 1)
                {
                    throw new ArgumentException("The specified revision number is not valid.", nameof(next));
                }

                var parentId = next.Parent?.Id;
                var parentRevision = next.Parent?.Revision;

                var delta = new
                {
                    Id = original.Id,
                    Revision = next.Revision,
                    Expires = next.Expires,
                    Expired = next.Expired,
                    Created = this.TimeService.Now(),
                    Payload = new JsonContainer(JsonConvert.SerializeObject(next.Payload)),
                    Script = next.Script,
                    ParentId = parentId,
                    ParentRev = parentRevision,
                    Status = (int)next.Status,
                    Error = new JsonContainer(JsonConvert.SerializeObject(next.Error)),
                };

                string query = $@"
update {Schema}.transactions set head = 'f' where id = @Id;
INSERT INTO {Schema}.transactions 
    (id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error) VALUES 
    (@Id, @Revision, @Created, @Expires, @Expired, @Payload, @Script, @ParentId, @ParentRev, @Status, @Error) RETURNING id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error;
";

                result = conn.ExecuteReader(query, delta, this.Map, trans).Single();
                trans.Commit();
            }

            this.OnTransactionCommitted(result);
            return result;
        }

        public override Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            return this.CreateTransactionAsync(transaction, null);
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction, NpgsqlTransaction sqlTransaction)
        {
            Transaction result;

            using (var cmd = await this.InsertTransactionCommandAsync())
            using (var trans = cmd.Connection.BeginTransaction())
            {
                using (var checkCmd = cmd.Connection.CreateCommand())
                {
                    checkCmd.Transaction = trans;
                    checkCmd.CommandText = $"select exists(select 1 from {Schema}.transactions_head where id = @id)";
                    var p = checkCmd.CreateParameter();
                    p.ParameterName = "id";
                    p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;
                    p.Value = transaction.Id;
                    checkCmd.Parameters.Add(p);
                    checkCmd.Prepare();
                    var exists = (bool)await checkCmd.ExecuteScalarAsync();
                    if (exists)
                    {
                        await trans.RollbackAsync();
                        throw new TransactionExistsException(transaction.Id);
                    }
                }

                cmd.Transaction = sqlTransaction;
                cmd.Parameters["id"].Value = transaction.Id;
                cmd.Parameters["revision"].Value = 1;
                cmd.Parameters["created"].Value = this.TimeService.Now();
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

                cmd.Parameters["status"].Value = (int)transaction.Status;

                using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                    {
                        throw new TransactionMissingException(transaction.Id);
                    }

                    result = this.Map(reader);
                }

                await trans.CommitAsync();
            }

            this.OnTransactionCommitted(result);
            return result;
        }

        public override async Task<Transaction> FetchTransactionAsync(Guid id, int revision = -1)
        {
            if (revision == -1)
            {
                using (var select = await this.SelectTransactionCommandAsync())
                {
                    select.Parameters["id"].Value = id;

                    using (var reader = (NpgsqlDataReader)await select.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                        {
                            throw new TransactionMissingException(id);
                        }

                        return this.Map(reader);
                    }
                }
            }

            using (var selectRevision = await this.SelectTransactionRevisionCommandAsync())
            {
                selectRevision.Parameters["id"].Value = id;
                selectRevision.Parameters["revision"].Value = revision;

                using (var revReader = (NpgsqlDataReader)await selectRevision.ExecuteReaderAsync())
                {
                    if (!revReader.Read())
                    {
                        throw new TransactionMissingException(id);
                    }

                    return this.Map(revReader);
                }
            }
        }

        public override async Task<IEnumerable<Transaction>> GetChainAsync(Guid id)
        {
            using (var selectChain = await this.SelectTransactionChainCommandAsync())
            {
                selectChain.Parameters["id"].Value = id;
                using (var reader = (NpgsqlDataReader)await selectChain.ExecuteReaderAsync())
                {
                    var results = new List<Transaction>();
                    while (reader.Read())
                    {
                        results.Add(this.Map(reader));
                    }

                    return results;
                }
            }
        }

        public override async Task<IEnumerable<Transaction>> GetChildTransactionsAsync(Guid transaction, params TransactionStatus[] statuses)
        {
            using (var cmd = await this.SelectChildTransactionsCommandAsync())
            {
                cmd.Parameters["parentId"].Value = transaction;
                cmd.Parameters["statuses"].Value = statuses;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var results = new List<Transaction>();
                    while (reader.Read())
                    {
                        results.Add(this.Map(reader));
                    }

                    return results;
                }
            }
        }

        public override async Task<IQueryable<Transaction>> QueryAsync()
        {
            return new PostgreSqlOrderedQuerableProvider<Transaction>(await this.GetQueryProviderAsync());
        }

        public override async Task<bool> TransactionExistsAsync(Guid id)
        {
            var conn = await this.GetConnectionAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"select exists(select 1 from {Schema}.transactions_head where id = @id)";
                var idPar = cmd.CreateParameter();
                idPar.Value = id;
                idPar.NpgsqlDbType = NpgsqlDbType.Uuid;
                idPar.ParameterName = "id";
                cmd.Parameters.Add(idPar);
                cmd.Prepare();
                return (bool)await cmd.ExecuteScalarAsync();
            }
        }

        public override void SaveTransactionState(Guid id, int revision, byte[] state)
        {
            var conn = this.GetConnection();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid);
                cmd.Parameters.Add("revision", NpgsqlTypes.NpgsqlDbType.Integer);
                cmd.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Bytea);
                cmd.Parameters["id"].Value = id;
                cmd.Parameters["revision"].Value = revision;
                cmd.Parameters["state"].Value = state;
                cmd.CommandText = $"INSERT INTO {Schema}.transaction_state (id, revision, state) VALUES (@id, @revision, @state)";
                cmd.ExecuteNonQuery();
            }
        }

        public override async Task SaveTransactionStateAsync(Guid id, int revision, byte[] state)
        {
            var conn = await this.GetConnectionAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid);
                cmd.Parameters.Add("revision", NpgsqlTypes.NpgsqlDbType.Integer);
                cmd.Parameters.Add("state", NpgsqlTypes.NpgsqlDbType.Bytea);
                cmd.Parameters["id"].Value = id;
                cmd.Parameters["revision"].Value = revision;
                cmd.Parameters["state"].Value = state;
                cmd.CommandText = $"INSERT INTO {Schema}.transaction_state (id, revision, state) VALUES (@id, @revision, @state)";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        protected override async Task<List<Transaction>> GetExpiringTransactionsInternal(CancellationToken cancel)
        {
            string sql = $"SELECT {SelectColumns} FROM {Schema}.transactions_head WHERE expires <= @now";
            var results = new List<Transaction>();

            using (var cmd = (await this.GetConnectionAsync()).CreateCommand())
            {
                cmd.CommandText = sql;
                var p = cmd.Parameters.Add(new NpgsqlParameter("now", NpgsqlDbType.Timestamp));
                p.Value = this.TimeService.Now();
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
                                results.Add(this.Map(reader));
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        failed = true;
                    }
                }
                while (failed);
            }

            this.SetNextExpiringTransactionTime(await this.GetNextExpiringTransactionTime());

            return results;
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

            data.Status = (TransactionStatus)reader.GetInt32(9);
            data.Error = reader.IsDBNull(10) ? null : JsonConvert.DeserializeObject<ExpandoObject>(reader.GetString(10));
            return new Transaction(data, this);
        }

        private async Task<DateTime?> GetNextExpiringTransactionTime()
        {
            string sql = $"SELECT expires FROM {Schema}.transactions_head WHERE expires IS NOT NULL ORDER BY expires ASC LIMIT 1";
            using (var cmd = (await this.GetConnectionAsync()).CreateCommand())
            {
                cmd.CommandText = sql;
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return null;
                }

                return (DateTime)result;
            }
        }

        private async Task<NpgsqlCommand> InsertTransactionCommandAsync()
        {
            NpgsqlCommand cmd = (await this.GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $@"
INSERT INTO {Schema}.transactions (id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error) 
VALUES (@id, @revision, @created, @expires, @expired, @payload, @script, @parentId, @parentRev, @status, @error)
RETURNING {SelectColumns};";
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("revision", NpgsqlDbType.Integer) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("created", NpgsqlDbType.Timestamp) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("expires", NpgsqlDbType.Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("expired", NpgsqlDbType.Timestamp) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("script", NpgsqlDbType.Text) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentId", NpgsqlDbType.Uuid) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("parentRev", NpgsqlDbType.Integer) { IsNullable = true });
            cmd.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Integer) { IsNullable = false });
            cmd.Parameters.Add(new NpgsqlParameter("error", NpgsqlDbType.Jsonb) { IsNullable = true });

            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionRevisionCommandAsync()
        {
            var cmd = (await this.GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM {Schema}.transactions WHERE id = @id AND revision = @revision";
            cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid));
            cmd.Parameters.Add(new NpgsqlParameter("revision", NpgsqlDbType.Integer));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionCommandAsync()
        {
            var cmd = (await this.GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM {Schema}.transactions_head WHERE id = @id";
            cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectTransactionChainCommandAsync()
        {
            var cmd = (await this.GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT {SelectColumns} FROM {Schema}.transactions WHERE id = @id ORDER BY revision ASC";
            cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid));
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlCommand> SelectChildTransactionsCommandAsync()
        {
            var cmd = (await this.GetConnectionAsync()).CreateCommand();
            cmd.CommandText = $"SELECT DISTINCT (id) FROM {Schema}.transactions_head WHERE parentId = @parentId AND status = ANY(@statuses)";
            cmd.Parameters.Add("parentId", NpgsqlDbType.Uuid);
            cmd.Parameters.Add("statuses", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlDbType.Integer);
            cmd.Prepare();
            return cmd;
        }

        private async Task<NpgsqlConnection> GetConnectionAsync()
        {
            NpgsqlConnection conn = null;
            var result = this.threadConnections.GetOrAdd(Thread.CurrentThread, thread => conn = this.CreateConnection());

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
            var result = this.threadConnections.GetOrAdd(Thread.CurrentThread, thread => conn = this.CreateConnection());

            if (conn != null && conn != result)
            {
                conn.Dispose();
            }

            if (result.State != ConnectionState.Open)
            {
                try
                {
                    result.Open();
                }
                catch (SocketException)
                {
                }
            }

            return result;
        }

        private async Task<PostgreSqlQueryProvider> GetQueryProviderAsync()
        {
            NpgsqlConnection conn = await this.GetConnectionAsync();
            return this.queryProviders.GetOrAdd(Thread.CurrentThread, thread => new PostgreSqlQueryProvider(conn, this));
        }

        private NpgsqlConnection CreateConnection()
        {
            var result = new NpgsqlConnection(this.connectionString);
            return result;
        }
    }
}
