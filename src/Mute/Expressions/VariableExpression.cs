// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class VariableExpression : Expression
    {
        public string Name { get; }

        public bool Mutable { get; }

        public VariableExpression(string name, bool mutable, DataType type, ParserRuleContext context)
             : base(type, context)
        {
            this.Name = name;
            this.Mutable = mutable;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
