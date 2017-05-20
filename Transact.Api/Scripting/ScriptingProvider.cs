using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Transact.Api;

namespace Transact.Scripting
{
    public class ScriptingProvider : IScriptRunner, IDisposable
    {
        private static readonly Regex LanguageTest = new Regex("#(?:(?<Language>lang=[^:&]+?:)|(?<Reference>ref=))", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<string, IScriptRunner> _runners;

        private readonly ConcurrentDictionary<string, Func<Transaction, Task<TransactionMutableData>>> _compiled;

        private readonly EventHandler<TransactionCommittedEventArgs> _transactionCommittedEventHandler;

        private readonly ITransactionStorage _storage;

        public ScriptingProvider(ITransactionStorage storage)
        {
            _transactionCommittedEventHandler = OnTransactionCommitted;
            _runners = new ConcurrentDictionary<string, IScriptRunner>(StringComparer.OrdinalIgnoreCase);
            _compiled = new ConcurrentDictionary<string, Func<Transaction, Task<TransactionMutableData>>>(StringComparer.OrdinalIgnoreCase);

            _compiled.TryAdd("hello", HelloWorld);
            _compiled.TryAdd("stage1", Stage1);
            _compiled.TryAdd("stage2", Stage2);
            _storage = storage;
        }

        private Task<TransactionMutableData> HelloWorld(Transaction tr)
        {
            TransactionMutableData m = new TransactionMutableData();
            m.Payload = tr.Payload;
            m.Error = tr.Error;
            m.Expires = null;
            m.Handler = tr.Handler;
            m.Script = tr.Script;
            m.State = TransactionState.Completed;

            Console.WriteLine("Hello World!");

            return Task.FromResult(m);
        }

        private Task<TransactionMutableData> Stage1(Transaction tr)
        {
            TransactionMutableData m = new TransactionMutableData();
            m.Payload = tr.Payload;
            m.Error = tr.Error;
            m.Expires = tr.Expires.Value.AddMinutes(1);
            m.Handler = tr.Handler;
            m.Script = "#ref=stage2";
            m.State = TransactionState.Authorized;

            Console.WriteLine("Hello World, Stage1!");

            return Task.FromResult(m);
        }

        private Task<TransactionMutableData> Stage2(Transaction tr)
        {
            TransactionMutableData m = new TransactionMutableData();
            m.Payload = tr.Payload;
            m.Error = tr.Error;
            m.Expires = null;
            m.Handler = tr.Handler;
            m.Script = null;
            m.State = TransactionState.Completed;

            Console.WriteLine("Hello World, Stage2!");

            return Task.FromResult(m);
        }

        public void AddLanguageRunner(string language, IScriptRunner runner)
        {
            _runners.AddOrUpdate(language, l => runner, (l, r) => runner);
        }

        public void RegisterLanguageProvider(string language, IScriptRunner runner)
        {
            _runners.TryAdd(language, runner);
        }

        public Task<TransactionMutableData> Run(string code, Transaction transaction)
        {
            var m = LanguageTest.Match(code);
            if (m.Success)
            {
                if (m.Groups["Language"].Success)
                {
                    string language = m.Groups["Language"].Value;
                    string script = code.Substring(m.Length);
                    return RunScript(language, script, transaction);
                }
                if (m.Groups["Reference"].Success)
                {
                    string reference = code.Substring(m.Length).Trim();
                    return RunReference(reference, transaction);
                }
            }
            return RunScript("C#", code, transaction);
        }

        private async void OnTransactionCommitted(object sender, TransactionCommittedEventArgs e)
        {
            if (e.Transaction.Payload != null)
            {
                var o = (IDictionary<string, object>)e.Transaction.Payload;
                object @internal;
                
                if (o.TryGetValue("@internal", out @internal) && "script".Equals(@internal))
                {
                    if (e.Transaction.State == TransactionState.Authorized)
                    {
                        await ProcessScriptTransaction(e.Transaction);
                    }
                }
            }
        }

        private async Task ProcessScriptTransaction(Transaction tr)
        {

            try
            {
                var payload = (IDictionary<string, object>) tr.Payload;

                string name = (string) payload["name"];

                if (tr.State == TransactionState.Authorized || tr.State == TransactionState.Completed)
                {
                    string language = (string) payload["language"];
                    string code = (string) payload["code"];

                    await RegisterScript(name, language, code);
                }
                else if(tr.State == TransactionState.Cancelled)
                {
                    RemoveReference(name);
                }
            }
            catch (Exception ex)
            {
                await _storage.CommitTransactionDelta(tr,
                    new Transaction(tr.Id, tr.Revision + 1, DateTime.UtcNow, null, null, tr.Payload, tr.Script,
                        TransactionState.Failed, tr.Parent, ex, _storage));
            }
        }

        public async Task Initialize()
        {
            var query = await _storage.Query();
            var scripts = query.Where(
                    tr => new JsonValue((IDictionary<string, object>) tr.Payload, "Payload", "@internal") == "script" && tr.State == TransactionState.Authorized)
                .ToArray();

            foreach (var script in scripts)
            {
                await ProcessScriptTransaction(script);
            }

            _storage.TransactionCommitted += _transactionCommittedEventHandler;
        }

        public Task<Func<Transaction, Task<TransactionMutableData>>> Compile(string code)
        {
            return Task.FromResult(new Func<Transaction, Task<TransactionMutableData>>(tr => Run(code, tr)));
        }

        public Task<TransactionMutableData> RunScript(string language, string script, Transaction transaction)
        {
            IScriptRunner runner;

            if (!_runners.TryGetValue(language, out runner))
            {
                throw new ArgumentException($"Could not locate a language runner named {language}.", nameof(language));
            }

            return runner.Run(script, transaction);

        }

        public Task<TransactionMutableData> RunReference(string reference, Transaction transaction)
        {

            Func<Transaction, Task<TransactionMutableData>> result;
            if (!_compiled.TryGetValue(reference, out result))
            {
                throw new ArgumentException($"Could not locate a script with name {reference}.", nameof(reference));
            }

            return result(transaction);
        }

        public async Task RegisterScript(string reference, string language, string script)
        {
            IScriptRunner runner;

            if (!_runners.TryGetValue(language, out runner))
            {
                throw new ArgumentException($"Could not locate a language runner name {language}.");
            }


            var func = await runner.Compile(script);

            _compiled.GetOrAdd(reference, r => func);
        }

        public void RemoveReference(string reference)
        {
            Func<Transaction, Task<TransactionMutableData>> res;
            _compiled.TryRemove(reference, out res);
        }

        public void Dispose()
        {
            
        }
    }
}
