// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class NamedArgument : Expression
    {
        public string Argument { get; }

        public Expression Value { get; }

        public NamedArgument(string argument, Expression value, ParserRuleContext context)
            : base(value.Type, context)
        {
            this.Argument = argument;
            this.Value = value;
        }

        public override string ToString()
        {
            return $"{this.Argument}: {this.Value}";
        }
    }
}
