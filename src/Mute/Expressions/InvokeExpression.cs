// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Antlr4.Runtime;

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
            this.Invokable = invokable;
            this.Arguments = arguments.ToList();
        }

        private string GetArgumentString()
        {
            return string.Join(", ", this.Arguments);
        }

        public override string ToString()
        {
            return $"{this.Invokable}({this.GetArgumentString()})";
        }
    }
}
