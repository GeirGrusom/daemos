using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class ConditionalExpression : Expression
    {
        public Expression Condition { get; }

        public Expression IfValue { get; }

        public Expression ElseValue { get; }

        public ConditionalExpression(Expression condition, Expression ifValue, Expression elseValue, ParserRuleContext context)
            : base(ifValue.Type == elseValue?.Type ? ifValue.Type : DataType.Void, context)
        {
            Condition = condition;
            IfValue = ifValue;
            ElseValue = elseValue;
        }
    }
}
