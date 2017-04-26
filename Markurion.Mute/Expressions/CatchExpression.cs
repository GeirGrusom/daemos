using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class CatchExpression : Expression
    {
        public Type Exception{ get; }

        public Expression Body { get; }

        public CatchExpression(Expression body, Type exception, ParserRuleContext context) : base(DataType.Void, context)
        {
            Exception = exception;
            Body = body;
        }
    }
}
