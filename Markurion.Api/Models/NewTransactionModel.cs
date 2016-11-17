using System;
using System.Dynamic;
using Newtonsoft.Json;

namespace Markurion.Api.Models
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
}