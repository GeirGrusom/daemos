// <copyright file="RoslynScriptRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos
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
}
