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
    /// Represents a query subscription
    /// </summary>
    public sealed class Subscription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="id">Subscription ID</param>
        /// <param name="predicate">Subscription predicate (usually compiled from a query)</param>
        /// <param name="callback">Predicate match callback</param>
        public Subscription(Guid id, Func<Transaction, bool> predicate, Action<Guid, Transaction> callback)
        {
            this.Id = id;
            this.Predicate = predicate;
            this.Callback = callback;
        }

        /// <summary>
        /// Gets the identifier for this subscription
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the predicate used to match this subscription
        /// </summary>
        internal Func<Transaction, bool> Predicate { get; }

        /// <summary>
        /// Gets the callback used to relay the transaction when it matches the predicate
        /// </summary>
        internal Action<Guid, Transaction> Callback { get; }
    }
}
