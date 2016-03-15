using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static System.Linq.Expressions.ExpressionType;

namespace Transact.Postgres
{
    public class PredicateQueryVisitor : ExpressionVisitor
    {
        private readonly StringBuilder builder;

        public PredicateQueryVisitor()
        {
            builder = new StringBuilder();
        }

        private static Dictionary<ExpressionType, string> binaryOperatorLookup = new Dictionary<ExpressionType, string>
        {
            [Add] = "+",
            [AddChecked] = "+",
            [Subtract] = "-",
            [SubtractChecked] = "-",
            [Multiply] = "*",
            [MultiplyChecked] = "*",
            [Divide] = "/",
            [Modulo] = "%",
            [And] = "&",
            [Or] = "|",
            [ExclusiveOr] = "#",
            [AndAlso] = "and",
            [OrElse] = "or",
            [LeftShift] = "<<",
            [RightShift] = ">>",
            [Power] = "^",
            [Equal] = "=",
            [NotEqual] = "<>",
            [GreaterThan] = ">",
            [GreaterThanOrEqual] = ">=",
            [LessThan] = "<",
            [LessThanOrEqual] = "<=",
        };

        private static Dictionary<ExpressionType, int> binaryPresedenseLookup = new Dictionary<ExpressionType, int>
        {
            [OrElse] = 0,
            [AndAlso] = 1,
            [Not] = 2,
            [Equal] = 3,
            [GreaterThan] = 4,
            [LessThan] = 4,
            [GreaterThanOrEqual] = 4,
            [LessThanOrEqual] = 4,
            // Like = 5,
            // OVERLAPS = 6,
            // Between = 7
            // In = 8,
            // ??? = 9
            // Not Null = 10,
            // == NUll = 11,
            // == True, == False, == Null, == Unkown = 12
            [Add] = 13,
            [Subtract] = 13,
            [Multiply] = 14,
            [Divide] = 14,
            [Modulo] = 14,
            [Power] = 15,
            [Negate] = 16,
            [ArrayIndex] = 17,
            [ExpressionType.Convert] = 18,
            [TypeAs] = 18,
            [MemberAccess] = 19
        };

        private static readonly MethodInfo LikeMethod = typeof(ObjectExtensions).GetMethod("Like", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        private static int GetExpressionPresedense(Expression exp)
        {
            int pres = 0;
            if (binaryPresedenseLookup.TryGetValue(exp.NodeType, out pres))
            {
                var binExp = exp as BinaryExpression;
                if(binExp != null)
                {
                    if(binExp.NodeType == Equal && binExp.Method == LikeMethod)
                    {
                        return 5;
                    }
                }
                return pres;
            }

            if(exp is MethodCallExpression)
            {
                UnaryExpression un = (UnaryExpression)exp;
            }
            return pres;
        }

        private static Dictionary<ExpressionType, string> unaryOperatorLookup = new Dictionary<ExpressionType, string>
        {
            [Negate] = "-",
            [Not] = "not",
            [Quote] = "",
        };

        protected override Expression VisitUnary(UnaryExpression node)
        {
            string op;
            if (!unaryOperatorLookup.TryGetValue(node.NodeType, out op))
                throw new NotSupportedException();

            if (node.NodeType != Quote)
                builder.Append($" {op}");

            Visit(node.Operand);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(string))
            {
                string value = node.Value.ToString().Replace("'", @"\'");
                builder.Append($"'{value}'");
            }
            else if (node.Type == typeof(DateTime))
            {
                DateTime value = (DateTime)node.Value;
                string output = value.ToUniversalTime().ToString("O");
                builder.Append($"timestamp '{output}'");
            }
            else if (node.Type == typeof(TimeSpan))
            {

                var ts = (TimeSpan)node.Value;

                var values = new List<string>();
                if (ts.Days != 0)
                    values.Add($"{ts.Days} days");
                if (ts.Hours != 0)
                    values.Add($"{ts.Hours} hours");
                if (ts.Minutes != 0)
                    values.Add($"{ts.Minutes} minutes");
                if (ts.Seconds != 0)
                    values.Add($"{ts.Seconds} seconds");

                builder.Append($"interval '{string.Join(" ", values)}'");
            }
            return node;
        }


        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(Transaction))
            {
                builder.Append($"\"{node.Member.Name}\"");
            }
            else
            {
                if (node.Member.DeclaringType == typeof(DateTime))
                {
                    VisitDateTime(node);
                    return node;
                }
                throw new NotImplementedException();
            }
            return node;
        }

        protected void VisitDateTime(MemberExpression node)
        {
            if (node.Member.Name == "Now" || node.Member.Name == "UtcNow")
            {
                builder.Append("timestamp (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')");
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(System.Math))
            {
                switch (node.Method.Name)
                {
                    case "Abs":
                        {
                            builder.Append("@(");
                            Visit(node.Arguments[0]);
                            builder.Append(")");
                            return node;
                        }
                }
            }
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            string op;
            if (!binaryOperatorLookup.TryGetValue(node.NodeType, out op))
                throw new NotSupportedException();

            if (op == "+" && (node.Left.Type == typeof(string) || node.Right.Type == typeof(string)))
                op = "||"; // String concat operator

            var requiresParens = GetExpressionPresedense(node.Left) < GetExpressionPresedense(node.Right);

            if(requiresParens)
                builder.Append("(");
            Visit(node.Left);
            builder.Append($" {op} ");
            Visit(node.Right);
            if(requiresParens)
                builder.Append(")");

            return node;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
