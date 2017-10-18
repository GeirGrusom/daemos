using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public sealed class RetryExpression : Expression
    {
        public RetryExpression(ParserRuleContext context) : base(DataType.Void, context)
        {
        }
    }
}
