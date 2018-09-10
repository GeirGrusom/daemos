// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// This class implements an ordered queryable for a PostgreSql LINQ query
    /// </summary>
    /// <typeparam name="TResult">Result to query by</typeparam>
    public class PostgreSqlOrderedQuerableProvider<TResult> : IOrderedQueryable<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlOrderedQuerableProvider{TResult}"/> class.
        /// </summary>
        /// <param name="provider">The query provider this instance is to be created for</param>
        public PostgreSqlOrderedQuerableProvider(PostgreSqlQueryProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.Expression = Expression.Constant(this);
            this.Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlOrderedQuerableProvider{TResult}"/> class.
        /// </summary>
        /// <param name="provider">The query provider this instance is to be created for</param>
        /// <param name="expression">The expression base for this provider</param>
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

        /// <inheritdoc/>
        public Type ElementType => typeof(Transaction);

        /// <inheritdoc/>
        public Expression Expression { get; }

        /// <inheritdoc/>
        public IQueryProvider Provider { get; }

        /// <inheritdoc/>
        public IEnumerator<TResult> GetEnumerator()
        {
            return this.Provider.Execute<IEnumerable<TResult>>(this.Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Provider.Execute<IEnumerable>(this.Expression).GetEnumerator();
        }
    }
}
