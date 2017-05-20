using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public class VariableDeclarationExpression : Expression
    {
        public VariableExpression Variable { get; }
        public Expression Assignment { get; }

        public VariableDeclarationExpression(VariableExpression variable, Expression assignment, ParserRuleContext context)
            : base(variable.Type, context)
        {
            Variable = variable;
            Assignment = assignment;
        }

        public override string ToString()
        {
            if (Assignment != null)
            {
                return $"{MutableString(Variable.Mutable)} {Variable.Name} <- {Assignment}";
            }
            return $"{MutableString(Variable.Mutable)} {Variable.Name} : {Variable.Type.Name}";
        }

        private static string MutableString(bool mutable)
        {
            return mutable ? "var" : "val";
        }
    }
}
