// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Represents a remainder expression <pre>%</pre>
    /// </summary>
    public sealed class BinaryRemainderExpression : BinaryExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryRemainderExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="context">Parser rule context used to make this expression</param>
        public BinaryRemainderExpression(Expression left, Expression right, ParserRuleContext context)
            : base(left, right, BinaryOperator.Remainder, context)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} % {this.Right}";
        }
    }
}
