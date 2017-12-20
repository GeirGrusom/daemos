// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class WithExpression : BinaryExpression
    {
        public WithExpression(Expression left, ObjectExpression right, ParserRuleContext context)
            : base(left, right, left.Type, BinaryOperator.With, context)
        {
        }

        public override string ToString()
        {
            return $"{this.Left} with {this.Right}";
        }
    }
}
