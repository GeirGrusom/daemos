using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Transact.Api.Models;
using System.Dynamic;

namespace Transact.Api
{

    public class NewTransactionModel
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }
        [JsonProperty("payload")]
        public ExpandoObject Payload { get; set; }
        [JsonProperty("expires")]
        public DateTime? Expires { get; set; }
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
        public DateTime? Expires { get; set; }
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


    public class TransactionController : ApiController
    {
        private readonly ITransactionStorage _storage;
        public TransactionController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public async Task<IHttpActionResult> Get(Guid id)
        {
            var transaction = await _storage.FetchTransaction(id);
            return Json(TransactionMapper.Map(transaction));
        }

        

        private static readonly string[] DateFormats =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'Z'"
        };
        public static bool TryParseDateTime(string dateTime, out DateTime? result)
        {
            DateTime res;
            
            if (dateTime == null)
            {
                result = null;
                return true;
            }
            if (string.Equals(dateTime, "tomorrow", StringComparison.Ordinal))
            {
                
                result = DateTime.UtcNow;
                return true;
            }
            if (string.Equals(dateTime, "tomorrow", StringComparison.Ordinal))
            {
                result = DateTime.UtcNow.AddDays(1);
                return true;
            }

            if (DateTime.TryParseExact(dateTime, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out res))
            {
                result = res;
                return true;
            }
            result = null;
            return false;

        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(Guid id, [FromBody] IDictionary<string, object> model)
        {
            var factory = new TransactionFactory(_storage);

            Transaction trans;
            try
            {
                trans = await factory.ContinueTransaction(id);
            }
            catch (TransactionMissingException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            Transaction result;
            try
            {
                DateTime? expires;
                if (model.ContainsKey("expires"))
                {
                    if (model["expires"] == null)
                        expires = null;
                    else
                    {
                        if (model["expires"] is DateTime)
                            expires = (DateTime) model["expires"];
                        else if (!TryParseDateTime(model["expires"] as string, out expires))
                            return
                                Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The specified expiration date could not be parsed as standard ISO UTC time.");
                    }
                }
                else
                    expires = trans.Expires;

                result = await trans.CreateDelta((ref TransactionMutableData data) =>
                {
                    data.Expires = expires;
                    data.Payload = model.ContainsKey("payload") ? model["payload"] : trans.Payload;
                    data.Script = model.ContainsKey("script") ? model["script"] as string : trans.Script;
                    data.Handler = model.ContainsKey("handler") ? model["handler"] as string : null;
                });
            }
            finally
            {
                await trans.Free();
            }

            return Request.CreateResponse(HttpStatusCode.Created, TransactionMapper.Map(result));
        }

    }
}
