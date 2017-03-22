using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Markurion.Api;
using Markurion.Api.Scripting;
using Markurion.Console.Configuration;
using Markurion.Modules;
using Markurion.Postgres;

namespace Markurion.Console
{
    public class Program
    {
        private static CancellationToken _cancel;
        private static TransactionHandlerFactory _handlerFactory;

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
                Listening = new ListenSettings {HttpPort = 5000, Host = "localhost", Scheme = Scheme.Http, WebSocketEnabled = true}
            };
            settings = parser.Parse(settings, args);

            ITransactionStorage storage = CreateStorageFromSettings(settings);
            try
            {
                System.Console.WriteLine("Initializing connection with the database provider...");
                await storage.Open();
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                System.Console.WriteLine(ex.Message);
                return;
            }
            var httpServer = new HttpServer(settings.Listening.BuildUri(), storage);

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var dir = Directory.CreateDirectory(Path.Combine(location, "Modules"));
            var modules = dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly);

            NativeRunner runner = new NativeRunner();

            foreach (var mod in modules)
            {
                try
                {
                    using (var fs = mod.OpenRead())
                    {
                        var asm = AssemblyLoadContext.Default.LoadFromStream(fs);

                        _handlerFactory.AddAssembly(asm);

                        var types = asm.ExportedTypes;

                        runner.RegisterModules(types, httpServer.Container.GetService);
                    }
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

            _handlerFactory = new TransactionHandlerFactory(type => (ITransactionHandler)httpServer.Container.GetService(type));

            ScriptingProvider provider = new ScriptingProvider(storage);
            provider.AddLanguageRunner("C#", new RoslynScriptRunner(_handlerFactory));
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
