using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daemos.Console.Configuration;
using Daemos.Installation;
using Daemos.Postgres.Installation;
using Npgsql;

namespace Daemos.Console
{
    public class Installer
    {
        public async Task Run(Settings settings)
        {
            var completedTasks = new Stack<ITask>();
            var connbuilder = new NpgsqlConnectionStringBuilder(settings?.ConnectionString);

            

            var postgresStep = new PostgresInstallerStep(connbuilder.Host, connbuilder.Port, new ConsoleCredentialsPrompt());
            foreach (var step in postgresStep.GetStepTasks())
            {
                try
                {
                    await step.Install();
                    if (step is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Task failed: {postgresStep.Name}");
                    System.Console.WriteLine(ex.Message);
                    while (completedTasks.Any())
                    {
                        System.Console.WriteLine($"  Rolling back {step.GetType().Name}");
                        var completedTask = completedTasks.Pop();
                        await completedTask.Rollback();
                    }
                    break;
                }
            }
            System.Console.WriteLine("Completed setting up the PostgreSQL database.");
        }
    }
}
