// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using Antlr4.Runtime;

    public class CatchExpression : Expression
    {
        public Type Exception { get; }

        public VariableExpression ExceptionValue { get; }

        public BlockExpression Body { get; }

        public CatchExpression(BlockExpression body, Type exception, ParserRuleContext context)
            : base(DataType.Void, context)
        {
            this.ExceptionValue = null;
            this.Exception = exception;
            this.Body = body;
        }

        public CatchExpression(BlockExpression body, VariableExpression exception, ParserRuleContext context)
            : base(DataType.Void, context)
        {
            this.ExceptionValue = exception;
            this.Exception = exception?.Type.ClrType ?? typeof(Exception);
            this.Body = body;
        }
    }
}
