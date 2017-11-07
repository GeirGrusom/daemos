// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    public enum UnaryOperator
    {
        Add,
        Subtract,
        Not,
        PrefixIncrement,
        PostfixIncrement,
        PrefixDecrement,
        PostfixDecrement,
        Await,
        Convert,
        NotNull,
        TimeSpan
    }

    public class UnaryExpression : Expression
    {
        public Expression Operand { get; }

        public UnaryOperator Operator { get; }

        protected UnaryExpression(DataType type, Expression operand, UnaryOperator @operator, ParserRuleContext context) : base(type, context)
        {
            this.Operand = operand;
            this.Operator = @operator;
        }

        protected UnaryExpression(Expression operand, UnaryOperator @operator, ParserRuleContext context) : base(operand?.Type ?? DataType.Void, context)
        {
            this.Operand = operand;
            this.Operator = @operator;
        }
    }

    public sealed class UnaryAddExpression : UnaryExpression
    {
        public UnaryAddExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Add, context)
        {
        }

        public override string ToString()
        {
            return $" + {this.Operand}";
        }
    }

    public sealed class UnarySubtractExpression : UnaryExpression
    {
        public UnarySubtractExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Subtract, context)
        {
        }

        public override string ToString()
        {
            return $" - {this.Operand}";
        }
    }

    public sealed class UnaryAwaitExpression : UnaryExpression
    {
        public UnaryAwaitExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Await, context)
        {
        }

        public override string ToString()
        {
            return $" await {this.Operand}";
        }
    }

    public sealed class UnaryNotExpression : UnaryExpression
    {
        public UnaryNotExpression(Expression operand, ParserRuleContext context) : base(DataType.Void, operand, UnaryOperator.Not, context)
        {
        }

        public override string ToString()
        {
            return $" not {this.Operand}";
        }
    }
}
