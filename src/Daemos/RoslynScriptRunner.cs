// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

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
            this.Now = DateTime.UtcNow;
            this.Today = new DateTime(this.Now.Year, this.Now.Month, this.Now.Day);
            this.Tomorrow = this.Today.AddDays(1);
            this.Handler = handler;
            this.Expires = null;
            this.Script = null;
            this.State = transaction.State;
            this.Payload = transaction.Payload ?? new System.Dynamic.ExpandoObject();
        }
    }
}
