// <copyright file="SubscriptionService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Daemos.Query;

    /// <summary>
    /// Service for subsribing to predicates
    /// </summary>
    public class SubscriptionService : IDisposable
    {
        private readonly EventHandler<TransactionCommittedEventArgs> eventHandler;
        private readonly ConcurrentDictionary<Guid, Subscription> subscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
        /// </summary>
        /// <param name="storage">Storage engine to retrieve transactions from</param>
        public SubscriptionService(ITransactionStorage storage)
        {
            this.Storage = storage;
            this.eventHandler = this.EventHandler;
            this.Storage.TransactionCommitted += this.eventHandler;
            this.subscriptions = new ConcurrentDictionary<Guid, Subscription>();
        }

        /// <summary>
        /// Gets the storage engine registered with this subscription service
        /// </summary>
        public ITransactionStorage Storage { get; }

        /// <summary>
        /// Lists and subscribes to a query. This will fetch all rows that match the query and any subsequent matches will also be returned.
        /// </summary>
        /// <param name="expression">The query to subscribe to</param>
        /// <param name="requestId">Request ID specified by client</param>
        /// <param name="callback">Callback function used to post transactions with</param>
        /// <returns>Subscription ID. used to unsubscribe and identify subscription when transactions are returned.</returns>
        public async Task<Guid> ListAndSubscribe(string expression, Guid requestId, Action<Guid, Transaction> callback)
        {
            var subscriptionId = requestId;
            var compiler = new MatchCompiler();
            var exp = compiler.BuildExpression(expression).Compile();
            var sub = new Subscription(subscriptionId, exp, callback);

            if (!this.subscriptions.TryAdd(subscriptionId, sub))
            {
                return requestId;
            }

            foreach (var trans in (await this.Storage.QueryAsync()).Where(exp))
            {
                callback(requestId, trans);
            }

            return subscriptionId;
        }

        /// <summary>
        /// Subscribes to an expression but does not retrieve old events.
        /// </summary>
        /// <param name="expression">The query to subscribe to</param>
        /// <param name="requestId">Request ID specified by client</param>
        /// <param name="callback">Callback function used to post transactions with</param>
        /// <returns>Subscription ID. used to unsubscribe and identify subscription when transactions are returned.</returns>
        public Guid Subscribe(string expression, Guid requestId, Action<Guid, Transaction> callback)
        {
            var subscriptionId = requestId;
            var compiler = new MatchCompiler();
            var exp = compiler.BuildExpression(expression).Compile();
            var sub = new Subscription(subscriptionId, exp, callback);
            if (!this.subscriptions.TryAdd(subscriptionId, sub))
            {
                throw new Exception();
            }

            return subscriptionId;
        }

        /// <summary>
        /// Unsubsribes from a subscription
        /// </summary>
        /// <param name="subscriptionId">Subscription ID retrieved when subscribed</param>
        public void Unsubscribe(Guid subscriptionId)
        {
            this.subscriptions.TryRemove(subscriptionId, out Subscription removedSubscription);
        }

        /// <summary>
        /// Disposes this subscription service and unsubscribes from the transaction engine.
        /// </summary>
        public void Dispose()
        {
            this.Storage.TransactionCommitted -= this.eventHandler;
        }

        private void EventHandler(object sender, TransactionCommittedEventArgs ev)
        {
            var exceptions = new List<Exception>();
            foreach (var item in this.subscriptions.ToArray())
            {
                if (!item.Value.Predicate(ev.Transaction))
                {
                    continue;
                }

                try
                {
                    item.Value.Callback(item.Key, ev.Transaction);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count == 0)
            {
                return;
            }

            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            throw new AggregateException(exceptions);
        }
    }
}
