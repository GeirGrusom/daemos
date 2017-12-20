// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;

    public class TryExpression : Expression
    {
        public TryExpression(Expression body, IEnumerable<CatchExpression> catchExpressions, Expression finallyExpression, ParserRuleContext context)
            : base(DataType.Void, context)
        {
            this.Body = body;
            this.CatchExpressions = catchExpressions.ToList();
            this.Finally = finallyExpression;
        }

        public Expression Body { get; }

        public List<CatchExpression> CatchExpressions { get; }

        public Expression Finally { get; }
    }
}
