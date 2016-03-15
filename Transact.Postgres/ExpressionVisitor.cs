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

        public PostgresVisitor()
        {
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
                        return node;
                    }
                }
            }
            return node;
        }

        public override string ToString()
        {
            return $"SELECT * FROM tr.\"Transactions\" WHERE {predicateVisitor.ToString()}";
        }
    }
}
