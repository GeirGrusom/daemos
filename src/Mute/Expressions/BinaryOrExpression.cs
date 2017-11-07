// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies a Binary OR expression
    /// </summary>
    public sealed class BinaryOrExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryOrExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">The parser context this expression was made from</param>
        public BinaryOrExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.Or, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} or {this.Right}";
        }
    }
}
