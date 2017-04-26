using System;
using Antlr4.Runtime;

namespace Markurion.Mute.Expressions
{
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
    }
    public class UnaryExpression : Expression
    {
        public Expression Operand { get; }

        public UnaryOperator Operator { get; }

        protected UnaryExpression(DataType type, Expression operand, UnaryOperator @operator, ParserRuleContext context) : base(type, context)
        {
            Operand = operand;
            Operator = @operator;
        }

        protected UnaryExpression(Expression operand, UnaryOperator @operator, ParserRuleContext context) : base(operand?.Type ?? DataType.Void, context)
        {
            Operand = operand;
            Operator = @operator;
        }
    }

    public sealed class UnaryAddExpression : UnaryExpression
    {
        public UnaryAddExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Add, context)
        {
        }

        public override string ToString()
        {
            return $" + {Operand}";
        }
    }

    public sealed class UnarySubtractExpression : UnaryExpression
    {
        public UnarySubtractExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Subtract, context)
        {
        }

        public override string ToString()
        {
            return $" - {Operand}";
        }
    }

    public sealed class UnaryAwaitExpression : UnaryExpression
    {
        public UnaryAwaitExpression(Expression operand, ParserRuleContext context) : base(operand, UnaryOperator.Await, context)
        {
        }

        public override string ToString()
        {
            return $" await {Operand}";
        }
    }

    public sealed class UnaryNotExpression : UnaryExpression
    {
        public UnaryNotExpression(Expression operand, ParserRuleContext context) : base(DataType.Void, operand, UnaryOperator.Not, context)
        {
        }

        public override string ToString()
        {
            return $" not {Operand}";
        }
    }
}
