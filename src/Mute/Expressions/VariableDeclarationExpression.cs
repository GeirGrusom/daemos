// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public class VariableDeclarationExpression : Expression
    {
        public VariableExpression Variable { get; }

        public Expression Assignment { get; }

        public VariableDeclarationExpression(VariableExpression variable, Expression assignment, ParserRuleContext context)
            : base(variable.Type, context)
        {
            this.Variable = variable;
            this.Assignment = assignment;
        }

        public override string ToString()
        {
            if (this.Assignment != null)
            {
                return $"{MutableString(this.Variable.Mutable)} {this.Variable.Name} <- {this.Assignment}";
            }
            return $"{MutableString(this.Variable.Mutable)} {this.Variable.Name} : {this.Variable.Type.Name}";
        }

        private static string MutableString(bool mutable)
        {
            return mutable ? "var" : "val";
        }
    }
}
