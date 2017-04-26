using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class WithExpression : BinaryExpression
    {
        public WithExpression(Expression left, ObjectExpression right, ParserRuleContext context) : base(left, right, left.Type, BinaryOperator.With, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} with {Right}";
        }
    }
}
