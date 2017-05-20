using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Transact.Api;
using Transact.Postgres;

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

        public static async Task MainApp(string[] args)
        {
            var storage = new MemoryStorage();//new PostgreSqlTransactionStorage();
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


            Thread listeningThread = new Thread(httpServer.Start)
            {
                Name = "Web Server"
            };

            listeningThread.Start();
            

            HandlerFactory = new TransactionHandlerFactory(type => (ITransactionHandler)httpServer.Container.GetService(type));

            TransactionProcessor processor = new TransactionProcessor(storage, new RoslynScriptRunner(HandlerFactory));

            


            //HandlerFactory.AddAssembly(Assembly.GetAssembly(typeof(Transaction)));


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
