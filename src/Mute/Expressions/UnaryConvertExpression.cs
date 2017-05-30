using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class UnaryConvertExpression : UnaryExpression
    {
        public UnaryConvertExpression(DataType resultType, Expression operand, ParserRuleContext context)
            : base(resultType, operand, UnaryOperator.Convert, context)
        {
        }

        public override string ToString()
        {
            return $" {Type.Name}!({Operand})";
        }
    }
}
