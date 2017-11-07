// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies a less than expression <pre>&lt;</pre>
    /// </summary>
    public sealed class BinaryLessExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryLessExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser context this expression was made from</param>
        public BinaryLessExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, DataType.NonNullBool, BinaryOperator.LessThan, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} < {this.Right}";
        }
    }
}
