using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class VariableExpression : Expression
    {
        public string Name { get; }
        public bool Mutable { get; }

        public VariableExpression(string name, bool mutable, DataType type, ParserRuleContext context)
             : base(type, context)
        {
            Name = name;
            Mutable = mutable;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
