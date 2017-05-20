using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

using Transact.Api.Models;

namespace Transact.Api.Controllers
{
    [Route("transactions/{id:guid}/{revision:min(0)}")]
    public class TransactionHandlingController : Controller
    {
        private readonly ITransactionStorage _storage;
        private const int BlockTimeMs = 2000;
        public TransactionHandlingController(ITransactionStorage storage)
        {
            _storage = storage;
        }

        public enum Blocking
        {
            Blocking,
            Nonblocking,
        }

        private async Task<IActionResult> Process(Guid id, int revision, string verb, TransactionState state, int timeout)
        {
            var factory = new TransactionFactory(_storage);
            Transaction trans;
            try
            {
                trans = await factory.ContinueTransaction(id, revision);
            }
            catch(TransactionConflictException)
            {
                return new StatusCodeResult(409);
            }
            catch (TransactionMissingException)
            {
                return NotFound();
            }
            Task<Transaction> waitTask;
            try
            {
                var delta = await trans.CreateDelta((ref TransactionMutableData data) =>
                {
                    data.Expires = DateTime.UtcNow;
                    data.Script = $"Handler.{verb}(Transaction);";
                });
                waitTask = _storage.WaitFor(x => x.Id == id && (x.State == state || x.State == TransactionState.Failed), timeout);
            }
            finally
            {
                await trans.Free();
            }

            if (timeout == 0)
                return new StatusCodeResult(202);

            var transaction = await waitTask;

            if (transaction == null)
                return new StatusCodeResult(202);

            return Created(Url.RouteUrl("TransactionGetRevision", new { id = transaction.Id, revision = transaction.Revision }), TransactionMapper.ToTransactionResult(transaction));
        }

        [HttpPost("complete", Name = "CompleteTransaction")]
        public Task<IActionResult> Complete(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return Process(id, revision, "Complete", TransactionState.Completed, wait);
        }

        [HttpPost("authorize", Name = "AuthorizeTransaction")]
        public Task<IActionResult> Authorize(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return Process(id, revision, "Authorize", TransactionState.Authorized, wait);
        }

        [HttpPost("cancel", Name = "CancelTransaction")]
        public Task<IActionResult> Cancel(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return Process(id, revision, "Cancel", TransactionState.Cancelled, wait);
        }
    }
}
