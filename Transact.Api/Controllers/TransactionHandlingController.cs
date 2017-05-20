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
    public class TransactionHandlingController : ApiController
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

        private async Task<HttpResponseMessage> Process(Guid id, string verb, TransactionState state, int timeout)
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
                return Request.CreateResponse(HttpStatusCode.Accepted);

            var transaction = await waitTask;

            if (transaction == null)
                return Request.CreateResponse(HttpStatusCode.Accepted);

            return Request.CreateResponse(HttpStatusCode.Created, TransactionMapper.Map(transaction));
        }

        [HttpPost]
        public Task<HttpResponseMessage> Complete(Guid id, [FromUri] int wait = BlockTimeMs)
        {
            return Process(id, "Complete", TransactionState.Completed, wait);
        }

        [HttpPost]
        public Task<HttpResponseMessage> Authorize(Guid id, [FromUri] int wait = BlockTimeMs)
        {
            return Process(id, "Authorize", TransactionState.Authorized, wait);
        }

        [HttpPost]
        public Task<HttpResponseMessage> Cancel(Guid id, [FromUri] int wait = BlockTimeMs)
        {
            return Process(id, "Cancel", TransactionState.Cancelled, wait);
        }
    }
}
