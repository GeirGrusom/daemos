// <copyright file="TransactionHandlingController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Daemos.WebApi.Models;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Specifies operations for dealing with transaction life times
    /// </summary>
    [Route("transactions/{id:guid}/{revision:min(0)}")]
    public class TransactionHandlingController : Controller
    {
        private const int BlockTimeMs = 2000;
        private readonly ITransactionStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionHandlingController"/> class.
        /// </summary>
        /// <param name="storage">Storage engine used by the controller</param>
        public TransactionHandlingController(ITransactionStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Specifies whether an operation should block or not
        /// </summary>
        public enum Blocking
        {
            /// <summary>
            /// The operation will block until one or more results are provided, or the operation times out.
            /// </summary>
            Blocking,

            /// <summary>
            /// The operation will non block and returns immediately
            /// </summary>
            Nonblocking,
        }

        /// <summary>
        /// Completes a transaction
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="revision">New transaction revision</param>
        /// <param name="wait">Block time in milliseconds</param>
        /// <returns>If the operation blocks it will return a transaction. Otherwise it will retrun 201 Created and a URL where the result will show up.</returns>
        [ProducesResponseType(typeof(TransactionResult), 201)]
        [HttpPost("complete", Name = "CompleteTransaction")]
        public Task<IActionResult> Complete(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return this.Process(id, revision, "Complete", TransactionStatus.Completed, wait);
        }

        /// <summary>
        /// Authorizes a transaction
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="revision">New transaction revision</param>
        /// <param name="wait">Block time in milliseconds</param>
        /// <returns>If the operation blocks it will return a transaction. Otherwise it will retrun 201 Created and a URL where the result will show up.</returns>        [ProducesResponseType(typeof(TransactionResult), 201)]
        [HttpPost("authorize", Name = "AuthorizeTransaction")]
        public Task<IActionResult> Authorize(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return this.Process(id, revision, "Authorize", TransactionStatus.Authorized, wait);
        }

        /// <summary>
        /// Cancels a transaction
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="revision">New transaction revision</param>
        /// <param name="wait">Block time in milliseconds</param>
        /// <returns>If the operation blocks it will return a transaction. Otherwise it will retrun 201 Created and a URL where the result will show up.</returns>
        [ProducesResponseType(typeof(TransactionResult), 201)]
        [HttpPost("cancel", Name = "CancelTransaction")]
        public Task<IActionResult> Cancel(Guid id, [FromRoute] int revision, [FromQuery] int wait = BlockTimeMs)
        {
            return this.Process(id, revision, "Cancel", TransactionStatus.Cancelled, wait);
        }

        private async Task<IActionResult> Process(Guid id, int revision, string verb, TransactionStatus state, int timeout)
        {
            var factory = new TransactionFactory(this.storage);
            Transaction trans;
            try
            {
                trans = await factory.ContinueTransactionAsync(id, revision);
            }
            catch (TransactionConflictException)
            {
                return new StatusCodeResult(409);
            }
            catch (TransactionMissingException)
            {
                return this.NotFound();
            }

            Task<Transaction> waitTask;
            try
            {
                var delta = await trans.CreateDelta(revision, false, (ref TransactionMutableData data) =>
                {
                    data.Expires = DateTime.UtcNow;
                    data.Script = $"Handler.{verb}(Transaction);";
                });
                waitTask = this.storage.WaitForAsync(x => x.Id == id && (x.Status == state || x.Status == TransactionStatus.Failed), timeout);
            }
            finally
            {
                await trans.Free();
            }

            if (timeout == 0)
            {
                return new StatusCodeResult(202);
            }

            var transaction = await waitTask;

            if (transaction == null)
            {
                return new StatusCodeResult(202);
            }

            return this.Created(this.Url.RouteUrl("TransactionGetRevision", new { id = transaction.Id, revision = transaction.Revision }), TransactionMapper.ToTransactionResult(transaction));
        }
    }
}
