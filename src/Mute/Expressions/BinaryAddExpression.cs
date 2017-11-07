// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies an Add expression <pre>+</pre>
    /// </summary>
    public sealed class BinaryAddExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryAddExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        public BinaryAddExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.Add, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryAddExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="meth">Method used to execute this add expression</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        public BinaryAddExpression(Expression left, Expression right, MethodInfo meth, ParserRuleContext context)
            : base(left, right, BinaryOperator.Add, meth, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} + {this.Right}";
        }
    }
}
