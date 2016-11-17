using Newtonsoft.Json;

namespace Markurion.Api.Models
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