using Newtonsoft.Json;

namespace Daemos.WebApi.Models
{
    public class ContinueTransactionModel
    {
        [JsonProperty("id")]
        public object Payload { get; set; }
        [JsonProperty("expires")]
        public string Expires { get; set; }
        [JsonProperty("script")]
        public string Script { get; set; }
    }
}