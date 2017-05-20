using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class LabelExpression : Expressions.Expression
    {
        public string Name { get; }
        public LabelExpression(DataType resultType, string name, ParserRuleContext context) : base(resultType, context)
        {
            Name = name;
        }
    }
}
