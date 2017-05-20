using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
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
