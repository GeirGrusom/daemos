// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Specifies an assignment expression <pre>&lt;-</pre>
    /// </summary>
    public sealed class BinaryAssignExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryAssignExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule context this expression is made from</param>
        public BinaryAssignExpression(VariableExpression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.Assign, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} <- {this.Right}";
        }
    }
}
