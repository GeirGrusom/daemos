// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Daemos.Postgres
{
    public class PostgreSqlOrderedQuerableProvider<TResult> : IOrderedQueryable<TResult>
    {
        public PostgreSqlOrderedQuerableProvider(PostgreSqlQueryProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.Expression = Expression.Constant(this);
            this.Provider = provider;
        }

        public PostgreSqlOrderedQuerableProvider(PostgreSqlQueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(IQueryable<TResult>).GetTypeInfo().IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            this.Expression = expression;
            this.Provider = provider;
        }

        public Type ElementType => typeof(Transaction);

        public Expression Expression { get; }

        public IQueryProvider Provider { get; }

        public IEnumerator<TResult> GetEnumerator()
        {
            return (this.Provider.Execute<IEnumerable<TResult>>(this.Expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this.Provider.Execute<IEnumerable>(this.Expression)).GetEnumerator();
        }

    }
}
