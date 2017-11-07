// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; }

        public Expression IfValue { get; }

        public Expression ElseValue { get; }

        public ConditionalExpression(Expression condition, Expression ifValue, Expression elseValue, ParserRuleContext context)
            : base(ifValue.Type == elseValue?.Type ? ifValue.Type : DataType.Void, context)
        {
            this.Condition = condition;
            this.IfValue = ifValue;
            this.ElseValue = elseValue;
        }
    }
}
