// <copyright file="TransactionRootController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Daemos.Scripting;
    using Daemos.WebApi.Models;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;

    /// <summary>
    /// This controller provides API's for the transaction root, I.e. creating new transactions, or querying
    /// </summary>
    [Route("transactions", Name = "TransactionRoot")]
    public class TransactionRootController : Controller
    {
        private readonly ITransactionStorage storage;
        private readonly IScriptRunner scriptRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRootController"/> class.
        /// </summary>
        /// <param name="storage">Storage engine used by the controller</param>
        /// <param name="scriptRunner">Scriptrunner used to compiler scripts before committing transaction</param>
        public TransactionRootController(ITransactionStorage storage, IScriptRunner scriptRunner)
        {
            this.storage = storage;
            this.scriptRunner = scriptRunner;
        }

        /// <summary>
        /// Creates a new transaction
        /// </summary>
        /// <param name="model">Transaction to create</param>
        /// <returns>The created transaction</returns>
        [ProducesResponseType(typeof(TransactionResult), 201)]
        [HttpPost(Name = "NewTransaction")]
        public async Task<IActionResult> Post([FromBody]NewTransactionModel model)
        {
            if (model == null)
            {
                return this.BadRequest();
            }

            if (model.Script != null)
            {
                try
                {
                    this.scriptRunner.Compile(model.Script);
                }
                catch (CompilationFailedException compilationFailed)
                {
                    return this.BadRequest(new
                    {
                        compilationFailed.Message,
                        compilationFailed.Errors
                    });
                }
            }

            var factory = new TransactionFactory(this.storage);

            Guid id = model.Id.GetValueOrDefault(Guid.NewGuid());

            if (!DateTimeParser.TryParseDateTime(model.Expires, out DateTime? expires))
            {
                return this.BadRequest();
            }

            var trans = await factory.CreateTransaction(id, expires, model.Payload, model.Script, null);

            var transResult = trans.ToTransactionResult();

            return this.Created(this.Url.RouteUrl("TransactionGetRevision", new { id = trans.Id, revision = trans.Revision }), transResult);
        }

        /// <summary>
        /// Queries the storage engine
        /// </summary>
        /// <param name="query">Query to run</param>
        /// <param name="skip">How many results to skip</param>
        /// <param name="take">How many results to take</param>
        /// <param name="rows">Sets a value indicating whether to return how many results total</param>
        /// <returns>Query results</returns>
        [ProducesResponseType(typeof(TransactionQueryResult), 200)]
        [HttpGet(Name = "TransactionQuery")]
        public async Task<IActionResult> Get([FromQuery] string query, [FromQuery] int? skip = null, [FromQuery] int? take = null, [FromQuery] bool rows = true)
        {
            if (string.IsNullOrEmpty(query))
            {
                return this.BadRequest("No query was specified.");
            }

            var compiler = new Query.MatchCompiler();
            var exp = compiler.BuildExpression(query);

            var whereQuery = (await this.storage.QueryAsync()).Where(exp);

            if (skip != null)
            {
                whereQuery = whereQuery.Skip(skip.Value);
            }

            if (take != null)
            {
                whereQuery = whereQuery.Take(take.Value);
            }

            var results = whereQuery.Select(TransactionMapper.ToTransactionResult).AsEnumerable();
            if (rows)
            {
                var count = (await this.storage.QueryAsync()).Where(exp).Count();
                return this.Json(new TransactionQueryResult(count, results));
            }

            return this.Json(new { results });
        }

        /// <summary>
        /// A model class to represent query results
        /// </summary>
        public class TransactionQueryResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TransactionQueryResult"/> class.
            /// </summary>
            /// <param name="count">Number of results total</param>
            /// <param name="results">Resultset</param>
            public TransactionQueryResult(int count, IEnumerable<TransactionResult> results)
            {
                this.Count = count;
                this.Results = results;
            }

            /// <summary>
            /// Gets the total number of rows the query hit
            /// </summary>
            [JsonProperty("count")]
            public int Count { get; }

            /// <summary>
            /// Gets the filtered results
            /// </summary>
            [JsonProperty("results")]
            public IEnumerable<TransactionResult> Results { get; }
        }
    }
}
