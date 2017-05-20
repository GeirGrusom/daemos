using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using static System.Linq.Expressions.ExpressionType;
namespace Transact.Postgres
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

        private static string defaultSelect = "\"Id\", \"Revision\", \"Created\", \"Expires\", \"Expired\", \"Payload\", \"Script\", \"ParentId\", \"ParentRevision\", \"State\"";

        public override string ToString()
        {
            if(count)
            {
                return $"SELECT COUNT(*) FROM tr.\"TransactionHead\" WHERE {predicateVisitor.ToString()};";
            }
            string limit = take != null ? "LIMIT " + take.Value : "";
            string offset = skip != null ? " OFFSET " + skip.Value : "";
            return $"SELECT {defaultSelect} FROM tr.\"TransactionHead\" WHERE {predicateVisitor.ToString()} {limit} {offset};";
        }
    }
}
