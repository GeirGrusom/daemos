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
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("revision")]
        public int Revision { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("payload")]
        public object Payload { get; set; }

        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }

        [JsonProperty("expired")]
        public DateTime? Expired { get; set; }
        
        [JsonProperty("state"), JsonConverter(typeof(StringEnumConverter))]
        public TransactionState State { get; set; }

        [JsonProperty("handler")]
        public string Handler { get; set; }
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
        public async Task<IActionResult> Get(Guid id)
        {
            var transaction = await _storage.FetchTransaction(id);
            return Json(TransactionMapper.Map(transaction));
        }
    }
}
