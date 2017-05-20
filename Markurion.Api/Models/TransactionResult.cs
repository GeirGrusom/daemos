using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Markurion.WebApi.Models
{
    public class TransactionResult
    {
        private readonly Transaction _transaction;

        public TransactionResult(Transaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
        }


        [JsonProperty("id")]
        public Guid Id => _transaction.Id;

        [JsonProperty("revision")]
        public int Revision => _transaction.Revision;

        [JsonProperty("created")]
        public DateTime Created => _transaction.Created;

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public object Payload => _transaction.Payload;

        [JsonProperty("expires", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Expires => _transaction.Expires;

        [JsonProperty("expired", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Expired => _transaction.Expired;

        [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
        public TransactionState State => _transaction.State;

        [JsonProperty("handler", NullValueHandling = NullValueHandling.Ignore)]
        public string Handler => _transaction.Handler;

        [JsonProperty("script", NullValueHandling = NullValueHandling.Ignore)]
        public string Script => _transaction.Script;

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public object Error => _transaction.Error;
    }
}