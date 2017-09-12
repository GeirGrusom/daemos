using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class ImportExpression : Expression
    {
        public string Name { get; }
        public ImportExpression(DataType resultType, string name, ParserRuleContext context) : base(resultType, context)
        {
            Name = name;
        }
    }
}
