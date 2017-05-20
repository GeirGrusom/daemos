using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transact.Api.Models;

namespace Transact.Api.Controllers
{
    [Route("transactions/{id:guid}")]
    public sealed class TransactionChainController : Controller
    {
        private readonly ITransactionStorage _storage;

        public TransactionChainController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        public async Task<IActionResult> Get(Guid id)
        {
            await _storage.LockTransaction(id);
            try
            {
                var transactions = await _storage.GetChain(id);

                return Json(transactions.Select(TransactionMapper.ToTransactionResult));
            }
            finally
            {
                await _storage.FreeTransaction(id);
            }
        }

        [HttpGet("{revision:min(0)}", Name = "TransactionGetRevision")]
        public async Task<IActionResult> Get(Guid id, [FromRoute] int revision)
        {
            try
            {
                await _storage.LockTransaction(id);
            }
            catch (TransactionMissingException)
            {
                return NotFound();
            }
            try
            {
                var transaction = await _storage.FetchTransaction(id, revision);

                return Json(transaction.ToTransactionResult());

            }
            finally
            {
                await _storage.FreeTransaction(id);
            }
        }

        [HttpPost("{revision:min(0)?}", Name = "TransactionPost")]
        public async Task<IActionResult> Post(Guid id, [FromRoute] int? revision, [FromBody] IDictionary<string, object> model)
        {
            var factory = new TransactionFactory(_storage);

            Transaction trans;
            if (revision != null)
            {
                trans = await factory.ContinueTransaction(id, (int)revision);
            }
            else
            {
                trans = await factory.ContinueTransaction(id);
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
                            expires = (DateTime)model["expires"];
                        else if (!DateTimeParser.TryParseDateTime(model["expires"] as string, out expires))
                        {
                            
                            return BadRequest("The specified expiration date could not be parsed as standard ISO UTC time.");
                        }
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

            return Created(Url.RouteUrl("TransactionGet", new { id = result.Id }), result.ToTransactionResult());
        }
    }
}
