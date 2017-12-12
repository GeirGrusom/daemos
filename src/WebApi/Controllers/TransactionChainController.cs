// <copyright file="TransactionChainController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Daemos.WebApi.Models;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// This controller handles chains (transaction revisions)
    /// </summary>
    [Route("transactions/{id:guid}/chain")]
    public sealed class TransactionChainController : Controller
    {
        private readonly ITransactionStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionChainController"/> class.
        /// </summary>
        /// <param name="storage">Storage engine used by controller</param>
        public TransactionChainController(ITransactionStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Get a transaction with all revisions
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <returns>List of transaction revisions</returns>
        [ProducesResponseType(typeof(TransactionResult[]), 200)]
        [ProducesResponseType(404)]
        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            if (!await this.storage.TransactionExistsAsync(id))
            {
                return this.NotFound();
            }

            await this.storage.LockTransactionAsync(id);
            try
            {
                var transactions = await this.storage.GetChainAsync(id);

                return this.Json(transactions.Select(TransactionMapper.ToTransactionResult));
            }
            finally
            {
                await this.storage.FreeTransactionAsync(id);
            }
        }

        /// <summary>
        /// Gets a transaction at the specified revision
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="revision">Revision number</param>
        /// <returns>Transaction for the specified revision</returns>
        [ProducesResponseType(typeof(TransactionResult), 200)]
        [HttpGet("{revision:min(0)}", Name = "TransactionGetRevision")]
        public async Task<IActionResult> Get(Guid id, [FromRoute] int revision)
        {
            try
            {
                await this.storage.LockTransactionAsync(id);
            }
            catch (TransactionMissingException)
            {
                return this.NotFound();
            }

            try
            {
                var transaction = await this.storage.FetchTransactionAsync(id, revision);

                if (transaction == null)
                {
                    return this.NotFound();
                }

                return this.Json(transaction.ToTransactionResult());
            }
            finally
            {
                await this.storage.FreeTransactionAsync(id);
            }
        }

        /// <summary>
        /// Creates a new transaction at the specified revision
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <param name="revision">Revision. Omitting this field assumes newest available revision</param>
        /// <param name="model">Transaction data</param>
        /// <returns>Returns the created transaction</returns>
        [ProducesResponseType(typeof(TransactionResult), 201)]
        [HttpPost("{revision:min(0)?}", Name = "TransactionPost")]
        public async Task<IActionResult> Post(Guid id, [FromRoute] int? revision, [FromBody] IDictionary<string, object> model)
        {
            var factory = new TransactionFactory(this.storage);

            Transaction trans;
            if (revision != null)
            {
                trans = await factory.ContinueTransactionAsync(id, (int)revision, -1);
            }
            else
            {
                trans = await factory.ContinueTransactionAsync(id);
            }

            Transaction result;
            try
            {
                DateTime? expires;
                if (model.ContainsKey("expires"))
                {
                    if (model["expires"] == null)
                    {
                        expires = null;
                    }
                    else
                    {
                        if (model["expires"] is DateTime)
                        {
                            expires = (DateTime)model["expires"];
                        }
                        else if (!DateTimeParser.TryParseDateTime(model["expires"] as string, out expires))
                        {
                            return this.BadRequest("The specified expiration date could not be parsed as standard ISO UTC time.");
                        }
                    }
                }
                else
                {
                    expires = trans.Expires;
                }

                TransactionState state;

                if (model.ContainsKey("state"))
                {
                    if (!Enum.TryParse((string)model["state"], false, out state))
                    {
                        return this.BadRequest("Could not parse transaction state.");
                    }
                }
                else
                {
                    state = trans.State;
                }

                result = await trans.CreateDelta(trans.Revision + 1, false, (ref TransactionMutableData data) =>
                {
                    data.Expires = expires;
                    data.Payload = model.ContainsKey("payload") ? model["payload"] : trans.Payload;
                    data.Script = model.ContainsKey("script") ? model["script"] as string : trans.Script;
                    data.Handler = model.ContainsKey("handler") ? model["handler"] as string : null;
                    data.State = state;
                });
            }
            finally
            {
                await trans.Free();
            }

            return this.Created(this.Url.RouteUrl("TransactionGet", new { id = result.Id }), result.ToTransactionResult());
        }
    }
}
