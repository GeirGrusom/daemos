// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Represents an expression for committing a transaction
    /// </summary>
    public sealed class CommitTransactionExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitTransactionExpression"/> class.
        /// </summary>
        /// <param name="transaction">Transaction to commit</param>
        /// <param name="isNewChild">Indicates whether this is a new child transaction or a revision</param>
        /// <param name="context">Parser context</param>
        public CommitTransactionExpression(Expression transaction, bool isNewChild, ParserRuleContext context)
            : base(new DataType(typeof(Transaction), false), context)
        {
            this.Transaction = transaction;
            this.IsNewChild = isNewChild;
        }

        /// <summary>
        /// Gets the transaction to commit
        /// </summary>
        public Expression Transaction { get; }

        /// <summary>
        /// Gets a value indicating whether this is a new child transaction rather than a new revision
        /// </summary>
        public bool IsNewChild { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"commit {this.Transaction}";
        }
    }
}
