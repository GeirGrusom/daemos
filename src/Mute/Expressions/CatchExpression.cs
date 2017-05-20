using System;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class CatchExpression : Expression
    {
        public Type Exception{ get; }

        public Expression Body { get; }

        public CatchExpression(Expression body, Type exception, ParserRuleContext context) : base(DataType.Void, context)
        {
            Exception = exception;
            Body = body;
        }
    }
}
