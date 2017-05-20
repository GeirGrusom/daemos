using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class WhileExpression : Expression
    {
        public Expression Condition { get; }
        public BlockExpression Contents { get; }
        public bool DoWhile { get; }
        public WhileExpression(Expression condition, BlockExpression contents, bool doWhile, ParserRuleContext context) : base(DataType.Void, context)
        {
            Condition = condition;
            Contents = contents;
            DoWhile = doWhile;
        }

        public override string ToString()
        {
            if (DoWhile)
            {
                return $"do {Contents} while({Condition})";
            }
            return $" while({Condition}) {{ {Contents} }}";
        }
    }
}
