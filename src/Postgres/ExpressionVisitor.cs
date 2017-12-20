// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Daemos.Postgres
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
            this.count = false;
            this.builder = new StringBuilder();
            this.predicateVisitor = new PredicateQueryVisitor();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(System.Linq.Queryable))
            {
                switch (node.Method.Name)
                {
                    case "Where":
                    {
                        // Argument 0 is the queryable.
                        this.predicateVisitor.Visit(node.Arguments[1]);
                        this.Visit(node.Arguments[0]);
                        return node;
                    }

                    case "Skip":
                    {
                        this.skip = (int)((ConstantExpression)node.Arguments[1]).Value;
                        this.Visit(node.Arguments[0]);
                        return node;
                    }

                    case "Take":
                    {
                        this.take = (int)((ConstantExpression)node.Arguments[1]).Value;
                        this.Visit(node.Arguments[0]);
                        return node;
                    }

                    case "Count":
                    {
                        this.count = true;
                        this.Visit(node.Arguments[0]);
                        return node;

                    }
                }
            }

            return node;
        }

        public IEnumerable<object> Parameters => this.predicateVisitor.Parameters;

        private static readonly string defaultSelect = "id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error";

        public override string ToString()
        {
            if (this.count)
            {
                return $"SELECT COUNT(*) FROM trans.transactions_head WHERE {this.predicateVisitor.ToString()};";
            }

            string limit = this.take != null ? "LIMIT " + this.take.Value : "";
            string offset = this.skip != null ? " OFFSET " + this.skip.Value : "";
            return $"SELECT {defaultSelect} FROM trans.Transactions_head WHERE {this.predicateVisitor.ToString()} {limit} {offset};";
        }
    }
}
