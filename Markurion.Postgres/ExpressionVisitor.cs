using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Markurion.Postgres
{
    public class PostgresVisitor : ExpressionVisitor
    {
        private readonly StringBuilder builder;

        private readonly PredicateQueryVisitor predicateVisitor;

        private int? skip;
        private int? take;
        private bool count;

        public PostgresVisitor()
        {
            count = false;
            builder = new StringBuilder();
            predicateVisitor = new PredicateQueryVisitor();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if(node.Method.DeclaringType == typeof(System.Linq.Queryable))
            {
                switch(node.Method.Name)
                {
                    case "Where":
                    {
                        // Argument 0 is the queryable.
                        predicateVisitor.Visit(node.Arguments[1]);
                        Visit(node.Arguments[0]);
                        return node;
                    }
                    case "Skip":
                    {
                        skip = (int)((ConstantExpression)node.Arguments[1]).Value;
                        Visit(node.Arguments[0]);
                        return node;
                    }
                    case "Take":
                    {
                        take = (int)((ConstantExpression)node.Arguments[1]).Value;
                        Visit(node.Arguments[0]);
                        return node;
                    }
                    case "Count":
                    {
                        count = true;
                        Visit(node.Arguments[0]);
                        return node;

                    }
                }
            }
            return node;
        }

        public IEnumerable<object> Parameters => predicateVisitor.Parameters;

        private static readonly string defaultSelect = "id, revision, created, expires, expired, payload, script, parentId, parentRevision, state, error";

        public override string ToString()
        {
            if(count)
            {
                return $"SELECT COUNT(*) FROM markurion.transactions_head WHERE {predicateVisitor.ToString()};";
            }
            string limit = take != null ? "LIMIT " + take.Value : "";
            string offset = skip != null ? " OFFSET " + skip.Value : "";
            return $"SELECT {defaultSelect} FROM markurion.Transactions_head WHERE {predicateVisitor.ToString()} {limit} {offset};";
        }
    }
}
