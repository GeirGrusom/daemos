using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class InvokeExpression : Expression 
    {
        public Expression Invokable { get; }

        public List<Expression> Arguments { get; }

        private static DataType GetTypeFromInvokable(Expression invokable)
        {
            if (invokable is MemberExpression)
            {
                return DataType.FromClrType(((MethodInfo) ((MemberExpression) invokable).Member).ReturnType);
            }
            throw new NotSupportedException();
        }

        public InvokeExpression(Expression invokable, IEnumerable<Expression> arguments, ParserRuleContext context)
            : base(GetTypeFromInvokable(invokable), context)
        {
            Invokable = invokable;
            Arguments = arguments.ToList();
        }

        private string GetArgumentString()
        {
            return string.Join(", ", Arguments);
        }

        public override string ToString()
        {
            return $"{Invokable}({GetArgumentString()})";
        }
    }
}
