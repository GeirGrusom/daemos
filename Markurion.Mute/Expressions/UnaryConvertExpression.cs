using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class UnaryConvertExpression : UnaryExpression
    {
        public UnaryConvertExpression(DataType resultType, Expression operand, ParserRuleContext context)
            : base(resultType, operand, UnaryOperator.Convert, context)
        {
        }

        public override string ToString()
        {
            return $" {Type.Name}({Operand})";
        }
    }
}
