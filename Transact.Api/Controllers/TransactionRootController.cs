using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Transact.Api.Models;

namespace Transact.Api.Controllers
{

    [Route("transactions", Name = "TransactionRoot")]
    public class TransactionRootController  : Controller //: ApiController
    {

        [HttpGet("foo")]
        public Task<IActionResult> foo()
        {

            return Task.FromResult<IActionResult>(NoContent());
        }

        private readonly ITransactionStorage _storage;

        public TransactionRootController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [HttpPost(Name = "NewTransaction")]
        public async Task<IActionResult> Post([FromBody]NewTransactionModel model)
        {
            if (model == null)
                return BadRequest();

            var factory = new TransactionFactory(_storage);

            Guid id = model.Id.GetValueOrDefault(Guid.NewGuid());

            DateTime? expires;


            if (!DateTimeParser.TryParseDateTime(model.Expires, out expires))
            {
                return BadRequest();
            }

            var trans = await factory.StartTransaction(id, expires, model.Payload, model.Script, null);
            await trans.Free();

            var transResult = TransactionMapper.Map(trans);

            return Created(Url.RouteUrl("TransactionGetRevision", new { id = trans.Id, revision = trans.Revision }), transResult);
        }

        [HttpGet(Name = "TransactionQuery")]
        public IActionResult Get([FromQuery] string query)
        {
            if(string.IsNullOrEmpty(query))
            {
                return BadRequest("No query was specified.");
            }

            var compiler = new Query.MatchCompiler();
            var exp = compiler.BuildExpression(query);

            var results = _storage.Query().Where(exp).Select(TransactionMapper.Map).AsEnumerable();

            return Json(new { results });
        }
    }
}
