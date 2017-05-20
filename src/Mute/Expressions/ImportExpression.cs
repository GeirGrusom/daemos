using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class ImportExpression : Expression
    {
        public ImportExpression(DataType resultType, ParserRuleContext context) : base(resultType, context)
        {
        }
    }
}
