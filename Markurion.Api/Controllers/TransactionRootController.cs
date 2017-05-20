using System;
using System.Linq;
using System.Threading.Tasks;
using Markurion.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Markurion.WebApi.Controllers
{

    [Route("transactions", Name = "TransactionRoot")]
    public class TransactionRootController  : Controller //: ApiController
    {
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

            if (!DateTimeParser.TryParseDateTime(model.Expires, out DateTime? expires))
            {
                return BadRequest();
            }

            
            var trans = await factory.CreateTransaction(id, expires, model.Payload, model.Script, null);

            var transResult = trans.ToTransactionResult();

            return Created(Url.RouteUrl("TransactionGetRevision", new { id = trans.Id, revision = trans.Revision }), transResult);
        }

        [HttpGet(Name = "TransactionQuery")]
        public async Task<IActionResult> Get([FromQuery] string query, [FromQuery] int? skip = null, [FromQuery] int? take = null, [FromQuery] bool rows = true)
        {
            if(string.IsNullOrEmpty(query))
            {
                return BadRequest("No query was specified.");
            }

            var compiler = new Query.MatchCompiler();
            var exp = compiler.BuildExpression(query);

            var whereQuery = (await _storage.QueryAsync()).Where(exp);

            if(skip != null)
            {
                whereQuery = whereQuery.Skip(skip.Value);
            }

            if(take != null)
            {
                whereQuery = whereQuery.Take(take.Value);
            }

            var results = whereQuery.Select(TransactionMapper.ToTransactionResult).AsEnumerable();
            if (rows)
            {
                var count = (await _storage.QueryAsync()).Where(exp).Count();
                return Json(new { count, results });
            }

            return Json(new { results });
        }
    }
}
