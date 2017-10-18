using Antlr4.Runtime;
using System;

namespace Daemos.Mute.Expressions
{
    public class CatchExpression : Expression
    {
        public Type Exception { get; }
        public VariableExpression ExceptionValue { get; }

        public BlockExpression Body { get; }

        public CatchExpression(BlockExpression body, Type exception, ParserRuleContext context) : base(DataType.Void, context)
        {
            ExceptionValue = null;
            Exception = exception;
            Body = body;
        }

        public CatchExpression(BlockExpression body, VariableExpression exception, ParserRuleContext context) : base(DataType.Void, context)
        {
            ExceptionValue = exception;
            Exception = exception?.Type.ClrType ?? typeof(Exception);
            Body = body;
        }
    }
}
