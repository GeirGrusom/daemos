﻿// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    public class PredicateQueryVisitor : ExpressionVisitor
    {
        private readonly StringBuilder builder;

        public List<object> Parameters { get; }

        private DateTime now;

        public PredicateQueryVisitor()
        {
            this.now = DateTime.UtcNow;
            this.builder = new StringBuilder();
            this.Parameters = new List<object>();
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
            [ExpressionType.Add] = "+",
            [ExpressionType.AddChecked] = "+",
            [ExpressionType.Subtract] = "-",
            [ExpressionType.SubtractChecked] = "-",
            [ExpressionType.Multiply] = "*",
            [ExpressionType.MultiplyChecked] = "*",
            [ExpressionType.Divide] = "/",
            [ExpressionType.Modulo] = "%",
            [ExpressionType.And] = "&",
            [ExpressionType.Or] = "|",
            [ExpressionType.ExclusiveOr] = "#",
            [ExpressionType.AndAlso] = "and",
            [ExpressionType.OrElse] = "or",
            [ExpressionType.LeftShift] = "<<",
            [ExpressionType.RightShift] = ">>",
            [ExpressionType.Power] = "^",
            [ExpressionType.Equal] = "=",
            [ExpressionType.NotEqual] = "<>",
            [ExpressionType.GreaterThan] = ">",
            [ExpressionType.GreaterThanOrEqual] = ">=",
            [ExpressionType.LessThan] = "<",
            [ExpressionType.LessThanOrEqual] = "<=",
        };

        private static Dictionary<ExpressionType, int> binaryPresedenseLookup = new Dictionary<ExpressionType, int>
        {
            [ExpressionType.OrElse] = 0,
            [ExpressionType.AndAlso] = 1,
            [ExpressionType.Not] = 2,
            [ExpressionType.Equal] = 3,
            [ExpressionType.GreaterThan] = 4,
            [ExpressionType.LessThan] = 4,
            [ExpressionType.GreaterThanOrEqual] = 4,
            [ExpressionType.LessThanOrEqual] = 4,
            // Like = 5,
            // OVERLAPS = 6,
            // Between = 7
            // In = 8,
            // ??? = 9
            // Not Null = 10,
            // == NUll = 11,
            // == True, == False, == Null, == Unkown = 12
            [ExpressionType.Add] = 13,
            [ExpressionType.Subtract] = 13,
            [ExpressionType.Multiply] = 14,
            [ExpressionType.Divide] = 14,
            [ExpressionType.Modulo] = 14,
            [ExpressionType.Power] = 15,
            [ExpressionType.Negate] = 16,
            [ExpressionType.ArrayIndex] = 17,
            [ExpressionType.Convert] = 18,
            [ExpressionType.TypeAs] = 18,
            [ExpressionType.MemberAccess] = 19
        };

        private static readonly MethodInfo LikeMethod = typeof(ObjectExtensions).GetMethod("Like", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        private static int GetExpressionPresedense(Expression exp)
        {
            if (binaryPresedenseLookup.TryGetValue(exp.NodeType, out int pres))
            {
                if (exp is BinaryExpression binExp)
                {
                    if (binExp.NodeType == ExpressionType.Equal && binExp.Method == LikeMethod)
                    {
                        return 5;
                    }
                }

                return pres;
            }

            if (exp is MethodCallExpression)
            {
                UnaryExpression un = (UnaryExpression)exp;
            }

            return pres;
        }

        private static Dictionary<ExpressionType, string> unaryOperatorLookup = new Dictionary<ExpressionType, string>
        {
            [ExpressionType.Negate] = "-",
            [ExpressionType.Not] = "not",
            [ExpressionType.Quote] = "",
            [ExpressionType.Convert] = ""
        };

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (!unaryOperatorLookup.TryGetValue(node.NodeType, out string op))
            {
                throw new NotSupportedException();
            }

            if (node.NodeType != ExpressionType.Quote)
            {
                this.builder.Append($" {op}");
            }

            this.Visit(node.Operand);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            this.builder.Append(":p" + (this.Parameters.Count + 1) + "");
            if (node.Value == null)
            {
                this.Parameters.Add(DBNull.Value);
            }
            else if (node.Value.GetType().GetTypeInfo().IsEnum)
            {
                this.Parameters.Add((int)node.Value);
            }
            else
            {
                this.Parameters.Add(node.Value);
            }

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type == typeof(JsonValue))
            {
                var memberOf = this.GetColumnName((string)((ConstantExpression)node.Arguments[1]).Value);
                var member = (string)((ConstantExpression)node.Arguments[2]).Value;
                this.builder.Append(memberOf);
                this.builder.Append(" ->> ");
                this.builder.Append('\'' + member + '\'');
            }

            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            this.builder.Append("(");
            for (int i = 0; i < node.Expressions.Count; ++i)
            {
                this.Visit(node.Expressions[i]);

                if (i < node.Expressions.Count - 1)
                {
                    this.builder.Append(", ");
                }
            }

            this.builder.Append(")");
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
                baseObj = this.ResolveMember(mem);
            }
            else
            {
                throw new NotImplementedException();
            }

            if (exp.Member is FieldInfo fi)
            {
                return fi.GetValue(baseObj);
            }

            if (exp.Member is PropertyInfo pi)
            {
                return pi.GetValue(baseObj);
            }

            throw new NotImplementedException();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(Transaction))
            {
                this.builder.Append(this.GetColumnName(node.Member.Name));
            }
            else
            {
                var objValue = this.ResolveMember(node);

                this.builder.Append(":p" + (this.Parameters.Count + 1) + "");
                this.Parameters.Add(objValue);
                return node;

                throw new NotImplementedException();
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(System.Linq.Enumerable))
            {
                switch (node.Method.Name)
                {
                    case "Contains":
                    {
                        this.Visit(node.Arguments[1]);
                        this.builder.Append(" in ");
                        this.Visit(node.Arguments[0]);
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
                            this.builder.Append("@(");
                            this.Visit(node.Arguments[0]);
                            this.builder.Append(")");
                            return node;
                        }

                    case "GetPayloadMember":
                        {
                            this.builder.Append("\"Payload\" -> ");
                            this.builder.Append(node.Arguments[1]);
                            return node;
                        }
                }
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (!binaryOperatorLookup.TryGetValue(node.NodeType, out string op))
            {
                throw new NotSupportedException();
            }

            if (op == "+" && (node.Left.Type == typeof(string) || node.Right.Type == typeof(string)))
            {
                op = "||"; // String concat operator
            }

            var requiresParens = GetExpressionPresedense(node.Left) < GetExpressionPresedense(node.Right);

            if (requiresParens)
            {
                this.builder.Append("(");
            }

            if (node.Left.Type == typeof(JsonValue))
            {
                this.builder.Append("(");
                this.Visit(node.Left);
                this.builder.Append(")");

                if (node.Right.Type == typeof(int) || node.Right.Type == typeof(long) || node.Right.Type == typeof(float) || node.Right.Type == typeof(double))
                {
                    this.builder.Append("::numeric");
                }
                else if (node.Right.Type == typeof(bool))
                {
                    this.builder.Append("::bool");
                }
            }
            else
            {
                this.Visit(node.Left);
            }

            this.builder.Append($" {op} ");
            this.Visit(node.Right);

            if (requiresParens)
            {
                this.builder.Append(")");
            }

            return node;
        }

        public override string ToString()
        {
            return this.builder.ToString();
        }
    }
}
