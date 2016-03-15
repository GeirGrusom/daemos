using Npgsql;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Transact.Postgres
{
    public class PostgreSqlQueryProvider : IQueryProvider
    {

        private readonly NpgsqlConnection connection;

        public PostgreSqlQueryProvider(NpgsqlConnection conn)
        {
            connection = conn;
        }

        private static Type GetElementType(Type expType)
        {
            var interf = expType.GetInterface("IEnumerable`1");
            return interf.GetGenericArguments()[0];
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return (IQueryable)Activator.CreateInstance(typeof(PostgreSqlOrderedQuerableProvider<>).MakeGenericType(GetElementType(expression.Type)), this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new PostgreSqlOrderedQuerableProvider<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            return null;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            return default(TResult);
        }
    }
}
