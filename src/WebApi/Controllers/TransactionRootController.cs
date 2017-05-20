using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Daemos.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Daemos.WebApi.Controllers
{

    [Route("transactions", Name = "TransactionRoot")]
    public class TransactionRootController  : Controller //: ApiController
    {
        private readonly ITransactionStorage _storage;

        public TransactionRootController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [ProducesResponseType(typeof(TransactionResult), 201)]
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

        public class TransactionQueryResult
        {
            public TransactionQueryResult(int count, IEnumerable<TransactionResult> results)
            {
                Count = count;
                Results = results;
            }

            [JsonProperty("count")]
            public int Count { get; }
            [JsonProperty("results")]
            public IEnumerable<TransactionResult> Results { get; }
        }

        [ProducesResponseType(typeof(TransactionQueryResult), 200)]
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
                return Json(new TransactionQueryResult(count, results));
            }

            return Json(new { results });
        }
    }
}
