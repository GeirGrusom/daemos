using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace Transact
{
    public class ScriptGlobals
    {
        public Transaction Transaction { get; }
        public Transaction Previous { get; }
        public ITransactionHandler Handler { get; }
        public DateTime Now { get; } = DateTime.UtcNow;
        public DateTime Today { get; } = DateTime.Today.ToUniversalTime();
        public DateTime Tomorrow { get; } = DateTime.Today.AddDays(1).ToUniversalTime();

        public ScriptGlobals(Transaction transaction, Transaction previous, ITransactionHandler handler)
        {
            Transaction = transaction;
            Previous = previous;
            Handler = handler;
        }
    }

    public class RoslynScriptRunner : IScriptRunner
    {
        private readonly ITransactionHandlerFactory _transactionHandlerFactory;

        public RoslynScriptRunner(ITransactionHandlerFactory transactionHandlerFactory)
        {
            _transactionHandlerFactory = transactionHandlerFactory;
        }

        public Task Run(string code, Transaction transaction, Transaction previous)
        {
            var options = ScriptOptions.Default
                .AddReferences("System.Runtime", "System")
                .AddReferences(Assembly.Load(new AssemblyName("Transact")))
                .AddImports("Transact", "System");
            
            return Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.RunAsync(code, options, new ScriptGlobals(transaction, previous, _transactionHandlerFactory.Get(transaction.Handler ?? "dummy")));
        }
    }
}
