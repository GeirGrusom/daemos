using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class ImportExpression : Expression
    {
        public ImportExpression(DataType resultType, ParserRuleContext context) : base(resultType, context)
        {
        }
    }
}
