// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class ImportExpression : Expression
    {
        public string Name { get; }

        public ImportExpression(DataType resultType, string name, ParserRuleContext context) : base(resultType, context)
        {
            this.Name = name;
        }
    }
}
