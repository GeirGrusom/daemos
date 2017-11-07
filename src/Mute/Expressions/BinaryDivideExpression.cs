// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies a divide expression <pre>/</pre>
    /// </summary>
    public sealed class BinaryDivideExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryDivideExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule this expression was made from</param>
        public BinaryDivideExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.Divide, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} / {this.Right}";
        }
    }
}
