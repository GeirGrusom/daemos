// <copyright file="PostgresInstallerStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Daemos.Installation;
using Npgsql;

namespace Daemos.Postgres.Installation
{
    public class PostgresInstallerStep : IInstallerStep
    {
        public string Name => "PostgreSQL support";

        public ICredentialsPrompt CredentialsPrompt { get; }
        
        public string Host { [return: CanBeNull] get; }

        public int Port { get; set; }

        public PostgresInstallerStep([CanBeNull]string host, int? port, ICredentialsPrompt credentialsPrompt)
        {
            CredentialsPrompt = credentialsPrompt;
            if (host != null)
            {
                var match = Regex.Match(host, @"^(?<HostName>.+):(?<Port>[0-9]+)$");
                if (match.Success)
                {
                    Host = match.Groups["HostName"].Value;
                    Port = int.Parse(match.Groups["Port"].Value);
                }
            }
            else
            {
                Port = port ?? 5432;
            }
        }

        public IEnumerable<ITask> GetStepTasks()
        {
            var selectDb = new SelectDatabaseTask(Host, Port, CredentialsPrompt);
            yield return selectDb;
            yield return new PostgresCreateDatabaseTask(selectDb.ConnectionString);
            yield return new InstallDatabaseSchemaTask(selectDb.ConnectionString, CredentialsPrompt);

        }
    }
}
