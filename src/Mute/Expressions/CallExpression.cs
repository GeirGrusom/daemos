// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies an expression that calls a static or instance method, or a constructor.
    /// </summary>
    public class CallExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallExpression"/> class.
        /// </summary>
        /// <param name="method">Specifies the method to call</param>
        /// <param name="instance">Specifies the instance to call this method on</param>
        /// <param name="arguments">Specifies the argument to call this method with</param>
        /// <param name="context">Specifies the parser context this expression was built from</param>
        public CallExpression(MethodInfo method, Expression instance, IEnumerable<Expression> arguments, ParserRuleContext context)
               : base(DataType.FromParameter(method.ReturnParameter), context)
        {
            this.Method = method;
            this.Instance = instance;
            this.Arguments = arguments.ToList();
            this.IsNamedArguments = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallExpression"/> class.
        /// </summary>
        /// <param name="method">Specifies the method to call</param>
        /// <param name="instance">Specifies the instance to call this method on</param>
        /// <param name="arguments">Specifies the argument to call this method with</param>
        /// <param name="context">Specifies the parser context this expression was built from</param>
        public CallExpression(MethodInfo method, Expression instance, IEnumerable<NamedArgument> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(method.ReturnType), context)
        {
            this.Method = method;
            this.Instance = instance;
            this.Arguments = arguments.Cast<Expression>().ToList();
            this.IsNamedArguments = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallExpression"/> class.
        /// </summary>
        /// <param name="ctor">Specifies the constructor this call expression is an invocation of</param>
        /// <param name="arguments">Specifies the argument to call this method with</param>
        /// <param name="context">Specifies the parser context this expression was built from</param>
        public CallExpression(ConstructorInfo ctor, IEnumerable<Expression> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(ctor.DeclaringType), context)
        {
            this.Method = ctor;
            this.Arguments = arguments.ToList();
            this.IsNamedArguments = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallExpression"/> class.
        /// </summary>
        /// <param name="ctor">Specifies the constructor this call expression is an invocation of</param>
        /// <param name="arguments">Specifies the argument to call this method with</param>
        /// <param name="context">Specifies the parser context this expression was built from</param>
        public CallExpression(ConstructorInfo ctor, IEnumerable<NamedArgument> arguments, ParserRuleContext context)
            : base(DataType.FromClrType(ctor.DeclaringType), context)
        {
            this.Method = ctor;
            this.Arguments = arguments.Cast<Expression>().ToList();
            this.IsNamedArguments = true;
        }

        /// <summary>
        /// Gets the method this call invokes
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// Gets the expression this call is made on
        /// </summary>
        public Expression Instance { get; }

        /// <summary>
        /// Gets a list of arguments to call this function with
        /// </summary>
        public List<Expression> Arguments { get; }

        /// <summary>
        /// Gets a value indicating whether this call uses named arguments
        /// </summary>
        public bool IsNamedArguments { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.Instance != null)
            {
                sb.Append(this.Instance);
            }
            else
            {
                if (this.Method is ConstructorInfo ctor)
                {
                    sb.Append(ctor.DeclaringType.Name);
                }
                else
                {
                    sb.Append(this.Method.Name);
                }
            }

            sb.Append("(");
            for (var index = 0; index < this.Arguments.Count; index++)
            {
                var arg = this.Arguments[index];
                sb.Append(arg);
                if (index < this.Arguments.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
