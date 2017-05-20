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
        public DateTime? Expires { get; set; }
        public string Script { get; set; }
        public TransactionState State { get; set; }
        public Transaction Transaction { get; }
        public ITransactionHandler Handler { get; }
        public dynamic Payload { get; set; }
        public DateTime Now { get; } = DateTime.UtcNow;
        public DateTime Today { get; } = DateTime.Today.ToUniversalTime();
        public DateTime Tomorrow { get; } = DateTime.Today.AddDays(1).ToUniversalTime();

        public ScriptGlobals(Transaction transaction, ITransactionHandler handler)
        {
            Now = DateTime.UtcNow;
            Today = new DateTime(Now.Year, Now.Month, Now.Day);
            Tomorrow = Today.AddDays(1);
            Handler = handler;
            Expires = null;
            Script = null;
            State = transaction.State;
            Payload = transaction.Payload ?? new System.Dynamic.ExpandoObject();
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

        public async Task<TransactionMutableData> Run(string code, Transaction transaction)
        {
            var global = new ScriptGlobals(transaction, _transactionHandlerFactory.Get(transaction.Handler ?? "dummy"));
            await CSharpScript.RunAsync(code, options, global);
            return new TransactionMutableData
            {
                Expires = global.Expires,
                State = global.State,
                Handler = transaction.Handler,
                Payload = global.Payload,
                Script = global.Script
            };
        }
    }
}
