// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public sealed class RetryExpression : Expression
    {
        public RetryExpression(ParserRuleContext context)
            : base(DataType.Void, context)
        {
        }
    }
}
