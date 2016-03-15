﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace Transact.Postgres
{
    public class PostgreSqlOrderedQuerableProvider<TResult> : IOrderedQueryable<TResult>
    {
        public PostgreSqlOrderedQuerableProvider(PostgreSqlQueryProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            Expression = Expression.Constant(this);
            Provider = provider;
        }

        public PostgreSqlOrderedQuerableProvider(PostgreSqlQueryProvider provider, Expression expression)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (!typeof(IQueryable<TResult>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException(nameof(expression));

            Expression = expression;
            Provider = provider;
        }

        public Type ElementType => typeof(Transaction);

        public Expression Expression { get; }

        public IQueryProvider Provider { get; }

        public IEnumerator<TResult> GetEnumerator()
        {
            return (Provider.Execute<IEnumerable<TResult>>(Expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (Provider.Execute<IEnumerable>(Expression)).GetEnumerator();
        }

    }
}
