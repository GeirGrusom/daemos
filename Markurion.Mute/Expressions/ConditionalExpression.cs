using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
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
