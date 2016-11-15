using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cons;
using Cons.Configuration;
using Transact.Api;
using Transact.Postgres;
using Transact.Scripting;

namespace Transact.Console
{
    public class Program
    {
        private static CancellationToken _cancel;
        private static TransactionHandlerFactory HandlerFactory;

        public static void Main(string[] args)
        {
            Nito.AsyncEx.AsyncContext.Run(() => MainApp(args));
        }

        private static ITransactionStorage CreateStorageFromSettings(Settings settings)
        {
            switch (settings.DatabaseType)
            {
                case DatabaseType.PostgreSql:
                    return new PostgreSqlTransactionStorage(settings.ConnectionString);
                case DatabaseType.Memory:
                    return new MemoryStorage();
                default:
                    throw new NotSupportedException("Storage mode not supported.");
            }
        }

        public static async Task MainApp(string[] args)
        {
            var parser = new ConfigurationParser();
            var settings = new Settings
            {
                DatabaseType = DatabaseType.Memory,
                Listening = new ListenSettings {HttpPort = 5000, Scheme = Scheme.Http, WebSocketEnabled = true}
            };
            settings = parser.Parse(settings, args);

            ITransactionStorage storage = CreateStorageFromSettings(settings);
            await storage.Open();
            var httpServer = new HttpServer(new Uri("http://localhost:8080/", UriKind.Absolute), storage);

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var dir = Directory.CreateDirectory(Path.Combine(location, "Modules"));
            var modules = dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            foreach (var mod in modules)
            {
                try
                {
                    //var asm = Assembly.LoadFile(mod.FullName);
                    //HandlerFactory.AddAssembly(asm);
                }
                catch (Exception )
                {
                    //System.Diagnostics.Trace.TraceError($"Unable to load assembly {mod}: {ex.Message}");
                }
            }

            var listeningThread = new Thread(httpServer.Start)
            {
                Name = "Web Server"
            };

            listeningThread.Start();

            HandlerFactory = new TransactionHandlerFactory(type => (ITransactionHandler)httpServer.Container.GetService(type));

            ScriptingProvider provider = new ScriptingProvider(storage);
            provider.AddLanguageRunner("C#", new RoslynScriptRunner(HandlerFactory));
            await provider.Initialize();

            TransactionProcessor processor = new TransactionProcessor(storage, provider);
         
            var cancelSource = new CancellationTokenSource();
            _cancel = cancelSource.Token;
            System.Console.CancelKeyPress += (sender, ev) =>
            {
                httpServer.Stop();
            };


            while (!cancelSource.IsCancellationRequested)
            {
                await processor.RunAsync(cancelSource.Token);
            }            


            httpServer.Wait();
        }        
    }
}
