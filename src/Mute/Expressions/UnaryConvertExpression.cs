// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class UnaryConvertExpression : UnaryExpression
    {
        public UnaryConvertExpression(DataType resultType, Expression operand, ParserRuleContext context)
            : base(resultType, operand, UnaryOperator.Convert, context)
        {
        }

        public override string ToString()
        {
            return $" {this.Type.Name}!({this.Operand})";
        }
    }
}
