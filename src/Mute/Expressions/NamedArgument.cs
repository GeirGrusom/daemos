using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class NamedArgument : Expression
    {
        public string Argument { get; }
        public Expression Value { get; }

        public NamedArgument(string argument, Expression value, ParserRuleContext context) : base(value.Type, context)
        {
            Argument = argument;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Argument}: {Value}";
        }
    }
}
