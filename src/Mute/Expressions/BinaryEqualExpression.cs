// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Specifes an equal comparison expression <pre>=</pre>
    /// </summary>
    public sealed class BinaryEqualExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryEqualExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        public BinaryEqualExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, DataType.NonNullBool, BinaryOperator.Equal, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryEqualExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="method">Method used by this comparison operation</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        public BinaryEqualExpression(Expression left, Expression right, MethodInfo method, ParserRuleContext context)
            : base(left, right, DataType.NonNullBool, BinaryOperator.Equal, method, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} = {this.Right}";
        }
    }
}
