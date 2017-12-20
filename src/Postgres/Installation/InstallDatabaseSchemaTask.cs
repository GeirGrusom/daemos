// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Daemos.Installation;
using Npgsql;
using NpgsqlTypes;

namespace Daemos.Postgres.Installation
{
    public class InstallDatabaseSchemaTask : PostgresTaskBase
    {
        private ICredentialsPrompt prompt;
        private NpgsqlConnectionStringBuilder connectionStringBuilder;

        public string ClientConnectionString { get; private set; }

        public InstallDatabaseSchemaTask(NpgsqlConnection connection, ICredentialsPrompt prompt)
            : base(connection)
        {
            this.connectionStringBuilder = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            this.prompt = prompt;
        }

        public InstallDatabaseSchemaTask(string connectionString, ICredentialsPrompt prompt)
            : base(connectionString)
        {
            this.connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            this.prompt = prompt;
        }

        protected override async Task OnInstall()
        {
            this.Connection.ChangeDatabase("daemos");
            var useCredentials = this.prompt.ReadCredentials("Please enter Daemos Postgres credentials");

            // I'm not entirely confident that this string is unescapable, but I would think it matters very little.
            string escapedUsername = useCredentials.UserName.Replace("\"", "\"\"");
            string escapedPassword = useCredentials.Password.Replace("'", "''");

            using (var cmd = this.Connection.CreateCommand())
            {
                cmd.Transaction = this.Transaction;
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

                // Can't use parameters for username or password because reasons.

                await cmd.ExecuteNonQueryAsync();

            }


            this.connectionStringBuilder.Username = useCredentials.UserName;
            this.connectionStringBuilder.Password = useCredentials.Password;
            this.connectionStringBuilder.Database = "daemos";

            this.ClientConnectionString = this.connectionStringBuilder.ToString();

            var storage = new PostgreSqlTransactionStorage(this.ClientConnectionString);

            dynamic payload = new ExpandoObject();
            ((IDictionary<string,object>)payload)["$$type"] = "Migration";
            ((IDictionary<string, object>)payload)["version"] = "1.0";


            var migrationTransaction = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, payload, null, TransactionStatus.Initialized, null, null, storage );
            try
            {
                await storage.CreateTransactionAsync(migrationTransaction, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
