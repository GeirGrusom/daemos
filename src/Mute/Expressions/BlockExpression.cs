// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;

    /// <summary>
    /// Represents a block of expressions
    /// </summary>
    public class BlockExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockExpression"/> class.
        /// </summary>
        /// <param name="statements">Statement body of this block</param>
        /// <param name="variables">Variables used inside this block</param>
        /// <param name="context">Parser rule context used to make this expression</param>
        public BlockExpression(IEnumerable<Expression> statements, IEnumerable<VariableExpression> variables, ParserRuleContext context)
            : base(DeduceReturnType(statements), context)
        {
            this.Body = statements?.ToList() ?? new List<Expression>();
            this.Variables = variables?.ToList() ?? new List<VariableExpression>();
        }

        /// <summary>
        /// Gets a list of variables used in this block expression
        /// </summary>
        public List<VariableExpression> Variables { get; }

        /// <summary>
        /// Gets a list of expressions which constitutes the body of this block expression
        /// </summary>
        public List<Expression> Body { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{\n{string.Concat(this.Body.Select(x => x.ToString() + ";\n"))}}}";
        }

        private static DataType DeduceReturnType(IEnumerable<Expression> exp)
        {
            if (exp == null)
            {
                return DataType.Void;
            }

            return DataType.Void;
        }
    }
}
