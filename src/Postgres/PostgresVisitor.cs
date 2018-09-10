// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;

    /// <summary>
    /// This class implememnts a base visitor to produce SQL from a LINQ expression
    /// </summary>
    public class PostgresVisitor : ExpressionVisitor
    {
        private static readonly string DefaultSelect = "id, revision, created, expires, expired, payload, script, parentId, parentRevision, status, error";

        private readonly StringBuilder builder;

        private readonly PredicateQueryVisitor predicateVisitor;

        private int? skip;
        private int? take;
        private bool count;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresVisitor"/> class.
        /// </summary>
        public PostgresVisitor()
        {
            this.count = false;
            this.builder = new StringBuilder();
            this.predicateVisitor = new PredicateQueryVisitor();
        }

        /// <summary>
        /// Gets a list of parameters for the SQL query
        /// </summary>
        public IEnumerable<object> Parameters => this.predicateVisitor.Parameters;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this.count)
            {
                return $"SELECT COUNT(*) FROM trans.transactions_head WHERE {this.predicateVisitor.ToString()};";
            }

            string limit = this.take != null ? "LIMIT " + this.take.Value : string.Empty;
            string offset = this.skip != null ? " OFFSET " + this.skip.Value : string.Empty;
            return $"SELECT {DefaultSelect} FROM trans.Transactions_head WHERE {this.predicateVisitor.ToString()} {limit} {offset};";
        }

        /// <summary>
        /// Visits a method call
        /// </summary>
        /// <param name="node">Node to visit</param>
        /// <returns><paramref name="node"/></returns>
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
    }
}
