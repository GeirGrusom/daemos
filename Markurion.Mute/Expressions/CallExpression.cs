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
        public MethodBase Method { get; }
        public Expression Instance { get; }
        public List<Expression> Arguments { get; }

        public bool IsNamedArguments { get; }

        public CallExpression(MethodInfo method, Expression instance, IEnumerable<Expression> arguments, ParserRuleContext context)
            : base(DataType.FromParameter(method.ReturnParameter), context)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments.ToList();
            IsNamedArguments = false;
        }

        public CallExpression(MethodInfo method, Expression instance, IEnumerable<NamedArgument> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(method.ReturnType), context)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments.Cast<Expression>().ToList();
            IsNamedArguments = true;
        }

        public CallExpression(ConstructorInfo ctor, IEnumerable<Expression> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(ctor.DeclaringType), context)
        {
            Method = ctor;
            Arguments = arguments.ToList();
            IsNamedArguments = false;
        }

        public CallExpression(ConstructorInfo ctor, IEnumerable<NamedArgument> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(ctor.DeclaringType), context)
        {
            Method = ctor;
            Arguments = arguments.Cast<Expression>().ToList();
            IsNamedArguments = true;
        }
    }
}
