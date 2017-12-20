// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Daemos.Installation;
using Npgsql;

namespace Daemos.Postgres.Installation
{
    public class SelectDatabaseTask : ITask
    {
        public string Host { get; }

        public int Port { get; }

        public SelectDatabaseTask(string host, int port, ICredentialsPrompt credentialsPrompt)
        {
            this.Host = host;
            this.Port = port;
            this.CredentialsPrompt = credentialsPrompt;
        }

        public ICredentialsPrompt CredentialsPrompt { get; }

        public string ConnectionString { get; private set; }

        public Task Install()
        {
            string host;
            if (this.Host == null)
            {
                Console.WriteLine("Please supply the host endpoint address.");
                host = Console.ReadLine();
                if (host == null)
                {
                    return Task.CompletedTask;
                }
            }
            else
            {
                host = this.Host;
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
            var credentials = this.CredentialsPrompt.ReadCredentials("Please supply systems administrator user credentials. This will be used to create the Daemos database. It is not used as application credentials.");
            connectionStringBuilder.Host = host;
            connectionStringBuilder.Database = "postgres";
            connectionStringBuilder.Username = credentials.UserName;
            connectionStringBuilder.Password = credentials.Password;
            this.ConnectionString = connectionStringBuilder.ToString();
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
