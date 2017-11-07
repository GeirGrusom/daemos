// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies a greater than or equal expression <pre>&gt;=</pre>
    /// </summary>
    public sealed class BinaryGreaterOrEqualExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryGreaterOrEqualExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser context this expression was produced from</param>
        public BinaryGreaterOrEqualExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, DataType.NonNullBool, BinaryOperator.GreaterThanOrEqual, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} >= {this.Right}";
        }
    }
}
