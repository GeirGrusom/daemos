using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
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
