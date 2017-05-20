using System;
using System.Linq;
using System.Threading.Tasks;
using Markurion.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Markurion.WebApi.Controllers
{
    [Route("transactions/{id:guid}")]
    public class TransactionController : Controller
    {
        private readonly ITransactionStorage _storage;
        public TransactionController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [HttpGet(Name = "TransactionGet")]
        public async Task<IActionResult> Get(Guid id, [FromQuery] bool history = false)
        {
            if(history)
            {
                var results = await _storage.GetChainAsync(id);
                return Json(results.Select(TransactionMapper.ToTransactionResult));
            }
            var transaction = await _storage.FetchTransactionAsync(id);
            return Json(transaction.ToTransactionResult());
        }
    }
}
