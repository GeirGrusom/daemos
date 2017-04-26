using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public class CallExpression : Expression
    {
        public MethodInfo Method { get; }
        public Expression Instance { get; }
        public List<Expression> Arguments { get; }

        public CallExpression(MethodInfo method, Expression instance, IEnumerable<Expression> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(method.ReturnType), context)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments.ToList();
        }
    }
}
