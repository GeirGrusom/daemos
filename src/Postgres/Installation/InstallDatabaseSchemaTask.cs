using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using Daemos.Installation;
using Npgsql;
using NpgsqlTypes;

namespace Daemos.Postgres.Installation
{
    public class InstallDatabaseSchemaTask : PostgresTaskBase
    {
        private ICredentialsPrompt _prompt;
        private NpgsqlConnectionStringBuilder _connectionStringBuilder;

        public string ClientConnectionString { get; private set; }
        public InstallDatabaseSchemaTask(NpgsqlConnection connection, ICredentialsPrompt prompt) : base(connection)
        {
            _connectionStringBuilder = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            _prompt = prompt;
        }

        public InstallDatabaseSchemaTask(string connectionString, ICredentialsPrompt prompt) : base(connectionString)
        {
            _connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            _prompt = prompt;
        }

        protected override void OnInstall()
        {
            Connection.ChangeDatabase("daemos");
            var useCredentials = _prompt.ReadCredentials("Please enter Daemos Postgres credentials");
            string escapedUsername = useCredentials.UserName.Replace("\"", "\"\"");
            string escapedPassword = useCredentials.Password.Replace("'", "''");
            using (var cmd = Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandText = $@"
create schema trans;

set search_path to trans;

create table transactions (
	id uuid not null,
	revision int not null,
	created timestamp without time zone not null,
	expires timestamp without time zone null,
	expired timestamp without time zone null,
	payload jsonb null,
	script text null,
	parentId uuid null,
	parentRevision int null,
	state integer not null,
	handler varchar(50),
	head boolean not null default(true),
	error jsonb null,
	constraint pk_transactions primary key (id, revision),
	constraint fk_transactions_parent foreign key (parentId, parentRevision) references transactions
);

create table transaction_state (
	id uuid not null,
	revision int not null,
	state bytea not null default E'',
	constraint pk_transaction_state primary key(id, revision),
	constraint fk_transaction_state_transaction foreign key (id, revision) references transactions
);

create view transactions_head as 
	select id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, handler, head, error 
	from transactions tr1
	where tr1.head = true;

create unique index index_transactions_head on transactions using btree (id, head) where head = true;

create index index_transactions_fk on transactions using btree (parentId, parentRevision) where parentId is not null;

create index index_transactions_id on transactions using btree (id);

create index index_transactions_expires on transactions using btree (expires) where expires is not null;

create role ""{escapedUsername}"" with login encrypted password '{escapedPassword}';

grant usage on schema trans to ""{escapedUsername}"";
grant select, update, insert on table transactions to ""{escapedUsername}"";
grant select, insert on table transaction_state to ""{escapedUsername}"";
grant select on table transactions_head to ""{escapedUsername}"";
";

                cmd.ExecuteNonQuery();

            }


            _connectionStringBuilder.Username = useCredentials.UserName;
            _connectionStringBuilder.Password = useCredentials.Password;

            ClientConnectionString = _connectionStringBuilder.ToString();

            var storage = new PostgreSqlTransactionStorage(ClientConnectionString);

            dynamic payload = new ExpandoObject();
            ((IDictionary<string,object>)payload)["$$type"] = "Migration";

            var migrationTransaction = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, payload, null, TransactionState.Initialized, null, null, storage );
            storage.CreateTransaction(migrationTransaction, null).GetAwaiter().GetResult();
        }
    }
}
