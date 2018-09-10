// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Daemos.Installation;
    using Npgsql;

    public class PostgresInstallerStep : IInstallerStep
    {
        public string Name => "PostgreSQL support";

        public ICredentialsPrompt CredentialsPrompt { get; }

        public string Host { [return: CanBeNull] get; }

        public int Port { get; set; }

        public PostgresInstallerStep([CanBeNull]string host, int? port, ICredentialsPrompt credentialsPrompt)
        {
            this.CredentialsPrompt = credentialsPrompt;
            if (host != null)
            {
                var match = Regex.Match(host, @"^(?<HostName>.+):(?<Port>[0-9]+)$");
                if (match.Success)
                {
                    this.Host = match.Groups["HostName"].Value;
                    this.Port = int.Parse(match.Groups["Port"].Value);
                }
            }
            else
            {
                this.Port = port ?? 5432;
            }
        }

        public IEnumerable<ITask> GetStepTasks()
        {
            var selectDb = new SelectDatabaseTask(this.Host, this.Port, this.CredentialsPrompt);
            yield return selectDb;
            yield return new PostgresCreateDatabaseTask(selectDb.ConnectionString);
            yield return new InstallDatabaseSchemaTask(selectDb.ConnectionString, this.CredentialsPrompt);

        }
    }
}
