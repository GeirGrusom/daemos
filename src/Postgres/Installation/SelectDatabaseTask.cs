using System;
using System.Collections.Generic;
using System.Text;
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
            Host = host;
            Port = port;
            CredentialsPrompt = credentialsPrompt;
        }

        public ICredentialsPrompt CredentialsPrompt { get; }
        public string ConnectionString { get; private set; }
        public void Install()
        {
            string host;
            if (Host == null)
            {
                Console.WriteLine("Please supply the host endpoint address.");
                host = Console.ReadLine();
                if (host == null)
                {
                    return;
                }
            }
            else
            {
                host = Host;
            }
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
            var credentials = CredentialsPrompt.ReadCredentials("Please supply systems administrator user credentials. This will be used to create the Daemos database. It is not used as application credentials.");
            connectionStringBuilder.Host = host;
            connectionStringBuilder.Database = "postgres";
            connectionStringBuilder.Username = credentials.UserName;
            connectionStringBuilder.Password = credentials.Password;
            ConnectionString = connectionStringBuilder.ToString();
        }

        public void Rollback()
        {
        }
    }
}
