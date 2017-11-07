// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class WhileExpression : Expression
    {
        public Expression Condition { get; }

        public BlockExpression Contents { get; }

        public bool DoWhile { get; }

        public WhileExpression(Expression condition, BlockExpression contents, bool doWhile, ParserRuleContext context) : base(DataType.Void, context)
        {
            this.Condition = condition;
            this.Contents = contents;
            this.DoWhile = doWhile;
        }

        public override string ToString()
        {
            if (this.DoWhile)
            {
                return $"do {this.Contents} while({this.Condition})";
            }
            return $" while({this.Condition}) {{ {this.Contents} }}";
        }
    }
}
