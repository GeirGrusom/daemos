using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Transact.Api;
using Transact.Postgres;

namespace Transact.Console
{
    class Program
    {
        private static CancellationToken _cancel;
        private static ITransactionStorage Storage;
        private static TransactionHandlerFactory HandlerFactory;
        static void Main(string[] args)
        {
            Storage = new PostgreSqlTransactionStorage();
            var unity = new UnityContainer();
            var httpServer = new HttpServer(new Uri("http://localhost:8080/", UriKind.Absolute), Storage, unity);

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var dir = Directory.CreateDirectory(Path.Combine(location, "Modules"));
            var modules = dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            foreach (var mod in modules)
            {
                try
                {
                    var asm = Assembly.LoadFile(mod.FullName);
                    HandlerFactory.AddAssembly(asm);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"Unable to load assembly {mod}: {ex.Message}");
                }
            }

            httpServer.Start();

            HandlerFactory = new TransactionHandlerFactory(type => (ITransactionHandler)unity.Resolve(type));
            unity.RegisterInstance<ITransactionHandlerFactory>(HandlerFactory);

            
            

            HandlerFactory.AddAssembly(Assembly.GetAssembly(typeof(Transaction)));


            var cancelSource = new CancellationTokenSource();

            _cancel = cancelSource.Token;

            System.Console.CancelKeyPress += (sender, ev) =>
            {
                cancelSource.Cancel();
                httpServer.Stop();
            };
            
            AsyncContext.Run(AsyncMain);
        }

        static async Task RequestQuery(ITransactionStorage storage, CancellationToken canel)
        {
            TransactionMatchCompiler compiler = new TransactionMatchCompiler();
            while (!canel.IsCancellationRequested)
            {
                string result = await System.Console.In.ReadLineAsync();
                var results = compiler.BuildExpression(result);

                var transactions = storage.Query().Where(results);

                foreach (var item in transactions)
                {
                    System.Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
                }
            }
        }

        static  async Task<int> AsyncMain()
        {
            
            var transactionProcessor = new TransactionProcessor(Storage, new RoslynScriptRunner(HandlerFactory));
            RequestQuery(transactionProcessor.Storage, _cancel);

            await transactionProcessor.RunAsync(_cancel);
            return 0;
        }
    }
}
