using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Transact.Api.Models;

namespace Transact.Api.Controllers
{
    public sealed class TransactionChainController : ApiController
    {
        private readonly ITransactionStorage _storage;

        public TransactionChainController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        public async Task<IHttpActionResult> Get(Guid id)
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
                var transactions = await _storage.GetChain(id);

                return Json(transactions.Select(TransactionMapper.Map));

            }
            finally
            {
                await _storage.FreeTransaction(id);
            }
        }

        public async Task<IHttpActionResult> Get(Guid id, int revision)
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

                return Json(TransactionMapper.Map(transaction));

            }
            finally
            {
                await _storage.FreeTransaction(id);
            }
        }
    }
}
