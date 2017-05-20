using System;
using System.Linq;
using System.Threading.Tasks;
using Daemos.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Daemos.WebApi.Controllers
{
    [Route("transactions/{id:guid}")]
    public class TransactionController : Controller
    {
        private readonly ITransactionStorage _storage;
        public TransactionController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        [ProducesResponseType(typeof(TransactionResult), 200)]
        [HttpGet(Name = "TransactionGet")]
        public async Task<IActionResult> Get(Guid id)
        {
            var transaction = await _storage.FetchTransactionAsync(id);
            return Json(transaction.ToTransactionResult());
        }
    }
}
