// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies a less than or equal expression <pre>&lt;=</pre>
    /// </summary>
    public sealed class BinaryLessOrEqualExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryLessOrEqualExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule context used to make this expression</param>
        public BinaryLessOrEqualExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, DataType.NonNullBool, BinaryOperator.LessThanOrEqual, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} <= {this.Right}";
        }
    }
}
