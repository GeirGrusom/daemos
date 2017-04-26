using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class TryExpression : Expression
    {
        public TryExpression(Expression body, IEnumerable<CatchExpression> catchExpressions, Expression finallyExpression, ParserRuleContext context) : base(DataType.Void, context)
        {
            Body = body;
            CatchExpressions = catchExpressions.ToList();
            Finally = finallyExpression;
        }

        public Expression Body { get; }

        public List<CatchExpression> CatchExpressions { get; }

        public Expression Finally { get; }
    }
}
