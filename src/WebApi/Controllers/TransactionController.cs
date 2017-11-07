// <copyright file="TransactionController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Daemos.WebApi.Models;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Specifies an API for fetching a single transaction
    /// </summary>
    [Route("transactions/{id:guid}")]
    public class TransactionController : Controller
    {
        private readonly ITransactionStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionController"/> class.
        /// </summary>
        /// <param name="storage">Storage engine used by the controller</param>
        public TransactionController(ITransactionStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Gets the transaction iwth the specified ID
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <returns>Transaction</returns>
        [ProducesResponseType(typeof(TransactionResult), 200)]
        [HttpGet(Name = "TransactionGet")]
        public async Task<IActionResult> Get(Guid id)
        {
            var transaction = await this.storage.FetchTransactionAsync(id);
            return this.Json(transaction.ToTransactionResult());
        }
    }
}
