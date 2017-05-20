﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Api
{

    public class Subscription
    {
        public Guid Id { get; }

        internal readonly Func<Transaction, bool> _predicate;

        internal readonly Action<Guid, Transaction> _callback;

        public Subscription(Guid id, Func<Transaction, bool> predicate, Action<Guid, Transaction> callback)
        {
            Id = id;
            _predicate = predicate;
            _callback = callback;
        }
    }

    public class SubscriptionService : IDisposable
    {
        private readonly EventHandler<TransactionCommittedEventArgs> _eventHandler;
        private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions; 
        public SubscriptionService(ITransactionStorage storage)
        {
            Storage = storage;
            _eventHandler = EventHandler;
            Storage.TransactionCommitted += _eventHandler;
            _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
        }

        private void EventHandler(object sender, TransactionCommittedEventArgs ev)
        {
            var exceptions = new List<Exception>();
            foreach (var item in _subscriptions.ToArray())
            {
                if (!item.Value._predicate(ev.Transaction))
                    continue;

                try
                {
                    item.Value._callback(item.Key, ev.Transaction);
                }
                catch(Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count == 0)
                return;
            if (exceptions.Count == 1)
                throw exceptions[0];
            throw new AggregateException(exceptions);
        }

        public async Task<Guid> ListAndSubscribe(Guid transactionId, Action<Guid, Transaction> callback)
        {

            await Storage.LockTransaction(transactionId);

            var subscriptionId = Guid.NewGuid();

            try
            {

                var sub = new Subscription(subscriptionId, x => x.Id == transactionId, callback);
                if (!_subscriptions.TryAdd(subscriptionId, sub))
                    throw new ApplicationException();

                var transactions = await Storage.GetChain(transactionId);

                foreach (var trans in transactions)
                {
                    callback(subscriptionId, trans);
                }

                return subscriptionId;
            }
            catch
            {
                Subscription value;
                _subscriptions.TryRemove(subscriptionId, out value);
                throw;
            }
            finally
            {
                await Storage.FreeTransaction(transactionId);
            }
        }

        public Guid Subscribe(string expression, Action<Guid, Transaction> callback)
        {
            var subscriptionId = Guid.NewGuid();
            var compiler = new TransactionMatchCompiler();
            var exp = compiler.BuildExpression(expression).Compile();
            var sub = new Subscription(subscriptionId, exp, callback);
            if (!_subscriptions.TryAdd(subscriptionId, sub))
                throw new ApplicationException();

            return subscriptionId;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            Subscription removedSubscription;
            if(!_subscriptions.TryRemove(subscriptionId,out removedSubscription))
                throw new KeyNotFoundException();
        }

        public ITransactionStorage Storage { get; }

        public void Dispose()
        {
            Storage.TransactionCommitted -= _eventHandler;
        }
    }
}
