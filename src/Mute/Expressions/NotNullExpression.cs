// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public sealed class NotNullExpression : UnaryExpression
    {
        public NotNullExpression(Expression operand, ParserRuleContext context)
            : base(new DataType(operand.Type.ClrType, false), operand, UnaryOperator.NotNull, context)
        {
        }

        public override string ToString()
        {
            return $"!!{this.Operand}";
        }
    }
}
