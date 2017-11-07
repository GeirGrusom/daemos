// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using Antlr4.Runtime;

    public class FunctionExpression : Expression
    {
        public List<VariableExpression> Parameters { get; }

        public BlockExpression Body { get; }

        public FunctionExpression(DataType resultType, ParserRuleContext context) : base(resultType, context)
        {
        }
    }
}
