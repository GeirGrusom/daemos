using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public sealed class CommitTransactionExpression : Expression
    {
        public Expression Transaction { get; }
        public bool IsNewChild { get; }

        public CommitTransactionExpression(Expression transaction,
            bool isNewChild, ParserRuleContext context) : base(new DataType(typeof(Transaction), false), context)
        {
            Transaction = transaction;
            IsNewChild = isNewChild;
        }

        public override string ToString()
        {
            return $"commit {Transaction}";
        }
    }
}
