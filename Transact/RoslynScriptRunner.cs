using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using System.Runtime;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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

        private static readonly ScriptOptions options = ScriptOptions.Default
            //.WithReferences(typeof(object).GetTypeInfo().Assembly)
            .WithReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
            .WithReferences("System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
            .WithImports("System");

        

        private unsafe ScriptOptions AddSystemRef(ScriptOptions options)
        {
            
            /*var assembly = typeof(object).GetTypeInfo().Assembly;
            byte* data;
            int length;
            if(assembly.TryGetRawMetadata(out data, out length))
            {
                var metaData = ModuleMetadata.CreateFromMetadata(new IntPtr(data), length);
                var reference = metaData.GetReference();

                return options.AddReferences(reference);
            }

            return options;*/
            var objectRef = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
            return options.AddReferences(objectRef);
            
        }

        public Task Run(string code, Transaction transaction, Transaction previous)
        {
            return CSharpScript.RunAsync(code, options, new ScriptGlobals(transaction, previous, _transactionHandlerFactory.Get(transaction.Handler ?? "dummy")));
        }
    }
}
