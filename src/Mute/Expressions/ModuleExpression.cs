// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;

    public class ModuleExpression : Expression
    {
        public string Name { get; }

        public BlockExpression Body { get; }

        public ModuleExpression(string name, IEnumerable<Expression> statements, ParserRuleContext context)
            : base(DataType.Void, context)
        {
            this.Name = name;
            this.Body = new BlockExpression(statements, statements.OfType<VariableExpression>(), context);
        }

        public override string ToString()
        {
            return $"module {this.Name};\n\n {this.Body}";
        }
    }
}
