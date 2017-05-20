using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Transact.Api.Models;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc;

namespace Transact.Api
{

    public class NewTransactionModel
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }
        [JsonProperty("payload")]
        public ExpandoObject Payload { get; set; }
        [JsonProperty("expires")]
        public string Expires { get; set; }
        [JsonProperty("script")]
        public string Script { get; set; }

        [JsonProperty("handler")]
        public string Handler { get; set; }
    }

    public class ContinueTransactionModel
    {
        [JsonProperty("id")]
        public object Payload { get; set; }
        [JsonProperty("expires")]
        public string Expires { get; set; }
        [JsonProperty("script")]
        public string Script { get; set; }
    }

    public class TransactionResult
    {
        private readonly Transaction _transaction;

        public TransactionResult(Transaction transaction)
        {
            if(transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
            }
            _transaction = transaction;
        }


        [JsonProperty("id")]
        public Guid Id => _transaction.Id;

        [JsonProperty("revision")]
        public int Revision => _transaction.Revision;

        [JsonProperty("created")]
        public DateTime Created => _transaction.Created;

        [JsonProperty("payload")]
        public object Payload => _transaction.Payload;

        [JsonProperty("expires")]
        public DateTime? Expires => _transaction.Expired;

        [JsonProperty("expired")]
        public DateTime? Expired => _transaction.Expired;

        [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
        public TransactionState State => _transaction.State;

        [JsonProperty("handler")]
        public string Handler => _transaction.Handler;
    }

    [Route("transactions/{id:guid}")]
    public class TransactionController : Controller
    {
        private readonly ITransactionStorage _storage;
        public TransactionController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [HttpGet(Name = "TransactionGet")]
        public async Task<IActionResult> Get(Guid id, [FromQuery] bool history = false)
        {
            if(history)
            {
                var results = await _storage.GetChain(id);
                return Json(results.Select(TransactionMapper.ToTransactionResult));
            }
            var transaction = await _storage.FetchTransaction(id);
            return Json(transaction.ToTransactionResult());

        }
    }
}
