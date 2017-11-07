// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies an logical AND expression <pre>&amp;&amp;</pre>
    /// </summary>
    public sealed class BinaryAndExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryAndExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser context this expression  was made from</param>
        public BinaryAndExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.And, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} and {this.Right}";
        }
    }
}
