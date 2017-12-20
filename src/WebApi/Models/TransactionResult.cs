// <copyright file="TransactionResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Models
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class TransactionResult
    {
        private readonly Transaction _transaction;

        public TransactionResult(Transaction transaction)
        {
            this._transaction = transaction ?? throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
        }

        [JsonProperty("id")]
        public Guid Id => this._transaction.Id;

        [JsonProperty("revision")]
        public int Revision => this._transaction.Revision;

        [JsonProperty("created")]
        public DateTime Created => this._transaction.Created;

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public object Payload => this._transaction.Payload;

        [JsonProperty("expires", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Expires => this._transaction.Expires;

        [JsonProperty("expired", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Expired => this._transaction.Expired;

        [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
        public TransactionStatus State => this._transaction.Status;

        [JsonProperty("handler", NullValueHandling = NullValueHandling.Ignore)]
        public string Handler => this._transaction.Handler;

        [JsonProperty("script", NullValueHandling = NullValueHandling.Ignore)]
        public string Script => this._transaction.Script;

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public object Error => this._transaction.Error;
    }
}