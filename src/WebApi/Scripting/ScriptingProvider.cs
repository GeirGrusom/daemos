// <copyright file="ScriptingProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Scripting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Daemos.Scripting;

    public class ScriptingProvider : IScriptRunner, IDisposable
    {
        private static readonly Regex LanguageTest = new Regex("#(?:lang=(?<Language>[^:&]+?):)|(?<Reference>ref=)", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<string, IScriptRunner> _runners;

        private readonly ConcurrentDictionary<string, Action<IDependencyResolver>> _compiled;

        private readonly EventHandler<TransactionCommittedEventArgs> _transactionCommittedEventHandler;

        private readonly ITransactionStorage _storage;

        public ScriptingProvider(ITransactionStorage storage)
        {
            this._transactionCommittedEventHandler = this.OnTransactionCommitted;
            this._runners = new ConcurrentDictionary<string, IScriptRunner>(StringComparer.OrdinalIgnoreCase);
            this._compiled = new ConcurrentDictionary<string, Action<IDependencyResolver>>(StringComparer.OrdinalIgnoreCase);

            this._storage = storage;
        }

        public void AddLanguageRunner(string language, IScriptRunner runner)
        {
            this._runners.AddOrUpdate(language, l => runner, (l, r) => runner);
        }

        public void RegisterLanguageProvider(string language, IScriptRunner runner)
        {
            this._runners.TryAdd(language, runner);
        }

        public void Run(string code, IDependencyResolver resolver)
        {
            var m = LanguageTest.Match(code);
            if (m.Success)
            {
                if (m.Groups["Language"].Success)
                {
                    string language = m.Groups["Language"].Value;
                    string script = code.Substring(m.Length);
                    this.RunScript(language, script, resolver);
                    return;
                }
                if (m.Groups["Reference"].Success)
                {
                    string reference = code.Substring(m.Length).Trim();
                    this.RunReference(reference, resolver);
                    return;
                }
            }
            this.RunScript("Mute", code, resolver);
        }

        private async void OnTransactionCommitted(object sender, TransactionCommittedEventArgs e)
        {
            if (e.Transaction.Payload != null)
            {
                var o = (IDictionary<string, object>)e.Transaction.Payload;

                if (o.TryGetValue("@internal", out object @internal) && "script".Equals(@internal))
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

                    this.RegisterScript(name, language, code);
                }
                else if (tr.State == TransactionState.Cancelled)
                {
                    this.RemoveReference(name);
                }
            }
            catch (Exception ex)
            {
                await this._storage.CommitTransactionDeltaAsync(tr,
                    new Transaction(tr.Id, tr.Revision + 1, DateTime.UtcNow, null, null, tr.Payload, tr.Script,
                        TransactionState.Failed, tr.Parent, ex, this._storage));
            }
        }

        public async Task Initialize()
        {
            var query = await this._storage.QueryAsync();
            var scripts = query.Where(
                    tr => new JsonValue((IDictionary<string, object>) tr.Payload, "Payload", "@internal") == "script" && tr.State == TransactionState.Authorized)
                .ToArray();

            foreach (var script in scripts)
            {
                await ProcessScriptTransaction(script);
            }

            this._storage.TransactionCommitted += this._transactionCommittedEventHandler;
        }

        public Action<IDependencyResolver> Compile(string code)
        {
            return new Action<IDependencyResolver>(tr => this.Run(code, tr));
        }

        public void RunScript(string language, string script, IDependencyResolver resolver)
        {
            if (!this._runners.TryGetValue(language, out IScriptRunner runner))
            {
                throw new ArgumentException($"Could not locate a language runner named {language}.", nameof(language));
            }

            runner.Run(script, resolver);

        }

        public void RunReference(string reference, IDependencyResolver resolver)
        {
            if (!this._compiled.TryGetValue(reference, out Action<IDependencyResolver> result))
            {
                throw new ArgumentException($"Could not locate a script with name {reference}.", nameof(reference));
            }

            result(resolver);
        }

        public void RegisterScript(string reference, string language, string script)
        {
            if (!this._runners.TryGetValue(language, out IScriptRunner runner))
            {
                throw new ArgumentException($"Could not locate a language runner name {language}.");
            }

            var func = runner.Compile(script);

            this._compiled.GetOrAdd(reference, r => func);
        }

        public void RemoveReference(string reference)
        {
            this._compiled.TryRemove(reference, out Action<IDependencyResolver> res);
        }

        public void Dispose()
        {
        }
    }
}
