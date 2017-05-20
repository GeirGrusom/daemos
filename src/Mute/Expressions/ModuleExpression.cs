using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class ModuleExpression : Expression 
    {
        public string Name { get; }

        public BlockExpression Body { get; }

        public ModuleExpression(string name, IEnumerable<Expression> statements, ParserRuleContext context)
            : base(DataType.Void, context)
        {
            Name = name;
            Body = new BlockExpression(statements, statements.OfType<VariableExpression>(), context);
        }

        public override string ToString()
        {
            return $"module {Name};\n\n {Body}";
        }
    }
}
