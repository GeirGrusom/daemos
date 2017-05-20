using System.Collections.Generic;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class FunctionExpression : Expression
    {
        public List<VariableExpression> Parameters { get; }

        public BlockExpression Body { get; }

        public FunctionExpression(DataType resultType, ParserRuleContext context) : base(resultType, context)
        {
        }
    }
}
