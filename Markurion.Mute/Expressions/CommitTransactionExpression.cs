using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
    public sealed class CommitTransactionExpression : Expression
    {
        public Expression Transaction { get; }
        public string State { get; }
        public bool IsNewChild { get; }

        public CommitTransactionExpression(Expression transaction, string state,
            bool isNewChild, ParserRuleContext context) : base(new DataType(typeof(Transaction), false), context)
        {
            Transaction = transaction;
            State = state;
            IsNewChild = isNewChild;
        }

        public override string ToString()
        {
            return $"{State} {Transaction}";
        }
    }
}
