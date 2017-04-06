using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static System.Linq.Expressions.ExpressionType;

namespace Markurion.Postgres
{
    public class PredicateQueryVisitor : ExpressionVisitor
    {
        private readonly StringBuilder builder;
        public List<object> Parameters { get; }

        private DateTime now;

        public PredicateQueryVisitor()
        {
            now = DateTime.UtcNow;
            builder = new StringBuilder();
            Parameters = new List<object>();
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        private static readonly Dictionary<string, string> tableNameLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "id",
            ["revision"] = "revision",
            ["created"] = "created",
            ["expires"] = "expires",
            ["expired"] = "expired",
            ["payload"] = "payload",
            ["script"] = "script",
            ["parentid"] = "parentId",
            ["parentrevision"] = "parentRevision",
            ["state"] = "state",
            ["handler"] = "handler"

        };

        public string GetColumnName(string columnname)
        {
            return tableNameLookup[columnname];
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
            [ExpressionType.Convert] = ""
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
            builder.Append(":p" + (Parameters.Count + 1) + "");
            if(node.Value == null)
            {
                Parameters.Add(DBNull.Value);
            }
            else if (node.Value.GetType().GetTypeInfo().IsEnum)
            {
                Parameters.Add((int)node.Value);
            }
            else
            {
                Parameters.Add(node.Value);
            }
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type == typeof(JsonValue))
            {
                var memberOf = GetColumnName((string)((ConstantExpression)node.Arguments[1]).Value);
                var member = (string)((ConstantExpression)node.Arguments[2]).Value;
                builder.Append(memberOf);
                builder.Append(" ->> ");
                builder.Append('\'' + member + '\'');
            }
            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            builder.Append("(");
            for(int i = 0; i < node.Expressions.Count; ++i)
            {
                Visit(node.Expressions[i]);

                if(i < node.Expressions.Count - 1)
                {
                    builder.Append(", ");
                }
            }
            builder.Append(")");
            return node;
        }

        protected object ResolveMember(MemberExpression exp)
        {
            object baseObj;
            if (exp.Expression is ConstantExpression cexp)
            {
                baseObj = cexp.Value;
            }
            else if (exp.Expression is MemberExpression mem)
            {
                baseObj = ResolveMember(mem);
            }
            else
            {
                throw new NotImplementedException();
            }
            if(exp.Member is FieldInfo fi)
            {
                return fi.GetValue(baseObj);
            }
            if(exp.Member is PropertyInfo pi)
            {
                return pi.GetValue(baseObj);
            }
            throw new NotImplementedException();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(Transaction))
            {
                builder.Append(GetColumnName(node.Member.Name));
            }
            else
            {
                if (node.Member.DeclaringType == typeof(DateTime))
                {
                    VisitDateTime(node);
                    return node;
                }
                var objValue = ResolveMember(node);
                    
                builder.Append(":p" + (Parameters.Count + 1) + "");
                Parameters.Add(objValue);
                return node;
                
                throw new NotImplementedException();
            }
            return node;
        }

        protected void VisitDateTime(MemberExpression node)
        {
            if (node.Member.Name == "Now" || node.Member.Name == "UtcNow")
            {
                builder.Append("timestamp ('" + now.ToString("yyyy-MM-dd'T'HH:mm:ss") + "' AT TIME ZONE 'UTC')");
            }
        }

        protected void VisitGuid(MemberExpression node)
        {
            if (node.Member.Name == "Now" || node.Member.Name == "UtcNow")
            {
                builder.Append("timestamp ('" + now.ToString("yyyy-MM-dd'T'HH:mm:ss") + "' AT TIME ZONE 'UTC')");
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(System.Linq.Enumerable))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                    {
                        Visit(node.Arguments[1]);
                        builder.Append(" in ");
                        Visit(node.Arguments[0]);
                        return node;
                    }
                }

            }
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
                    case "GetPayloadMember":
                        {
                            builder.Append("\"Payload\" -> ");
                            builder.Append(node.Arguments[1]);
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

            if (node.Left.Type == typeof(JsonValue))
            {
                builder.Append("(");
                Visit(node.Left);
                builder.Append(")");

                if (node.Right.Type == typeof(int) || node.Right.Type == typeof(long) || node.Right.Type == typeof(float) || node.Right.Type == typeof(double))
                {
                    builder.Append("::numeric");
                }
                else if(node.Right.Type == typeof(bool))
                {
                    builder.Append("::bool");
                }
            }
            else
            {
                Visit(node.Left);
            }
            builder.Append($" {op} ");
            Visit(node.Right);

            if (requiresParens)
                builder.Append(")");

            return node;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
