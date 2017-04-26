using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion.Mute.Expressions
{
    public sealed class NotNullExpression : UnaryExpression
    {
        public NotNullExpression(Expression operand, ParserRuleContext context)
            : base(new DataType(operand.Type.ClrType, false), operand, UnaryOperator.NotNull, context)
        {
        }

        public override string ToString()
        {
            return $"!!{Operand}";
        }
    }
}
