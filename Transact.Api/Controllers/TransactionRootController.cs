using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Transact.Api.Models;

namespace Transact.Api.Controllers
{
    
    public class TransactionRootController : ApiController
    {

        private readonly ITransactionStorage _storage;

        public TransactionRootController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody]NewTransactionModel model)
        {
            var factory = new TransactionFactory(_storage);

            Guid id = model.Id.GetValueOrDefault(Guid.NewGuid());

            var trans = await factory.StartTransaction(id);
            Transaction result;
            try
            {
                result = await trans.CreateDelta((ref TransactionMutableData data) =>
                {
                    data.Expires = model.Expires;
                    data.Payload = model.Payload;
                    data.Script = model.Script;
                    data.Handler = model.Handler;
                });
            }
            finally
            {
                await trans.Free();
            }
            
            var response = Request.CreateResponse(HttpStatusCode.Created, TransactionMapper.Map(result));

            response.Headers.Add("Location", Url.Route("SpecificTransaction", new { id = result.Id.ToString("N") }));
            return response;
        }

        [HttpGet]
        public IHttpActionResult Get([FromUri] string query)
        {

            TransactionMatchCompiler compiler = new TransactionMatchCompiler();
            var exp = compiler.BuildExpression(query);

            var results = _storage.Query().Where(exp).Select(TransactionMapper.Map).AsEnumerable();

            return Json(new { results });
        }
    }
}
