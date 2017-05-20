using System;
using Antlr4.Runtime;
using System.Reflection;

namespace Markurion.Mute.Expressions
{
    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Remainder,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        And,
        Or,
        ExclusiveOr,
        Assign,
        With
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public BinaryOperator Operator { get; }

        public MethodInfo Method { [return: CanBeNull]get; }

        protected BinaryExpression(Expression left, Expression right, BinaryOperator @operator, ParserRuleContext context) : this(left, right, @operator, null, context)
        {
        }

        protected BinaryExpression(Expression left, Expression right, BinaryOperator @operator, MethodInfo method, ParserRuleContext context) : base(left.Type, context)
        {
            Method = method;
            Operator = @operator;
            Left = left;
            Right = right;
        }

        protected BinaryExpression(Expression left, Expression right, DataType resultType, BinaryOperator @operator, MethodInfo method, ParserRuleContext context) : base(resultType, context)
        {
            Method = method;
            Operator = @operator;
            Left = left;
            Right = right;
        }

        protected BinaryExpression(Expression left, Expression right, DataType resultType, BinaryOperator @operator, ParserRuleContext context) : this(left, right, resultType, @operator, null, context)
        {
            Method = null;
            Operator = @operator;
            Left = left;
            Right = right;
        }

    }

    public sealed class BinaryAddExpression : BinaryExpression
    {
        public BinaryAddExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Add, context)
        {
        }

        public BinaryAddExpression(Expression left, Expression right, MethodInfo meth, ParserRuleContext context) : base(left, right, BinaryOperator.Add, meth, context)
        {
        }


        public override string ToString()
        {
            return $"{Left} + {Right}";
        }
    }

    public sealed class BinarySubtractExpression : BinaryExpression
    {
        public BinarySubtractExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Subtract, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} - {Right}";
        }

    }

    public sealed class BinaryMultiplyExpression : BinaryExpression
    {
        public BinaryMultiplyExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Multiply, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} * {Right}";
        }

    }

    public sealed class BinaryDivideExpression : BinaryExpression
    {
        public BinaryDivideExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Divide, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} / {Right}";
        }

    }

    public sealed class BinaryRemainderExpression : BinaryExpression
    {
        public BinaryRemainderExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Remainder, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} % {Right}";
        }

    }

    public sealed class BinaryEqualExpression : BinaryExpression
    {
        public BinaryEqualExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.Equal, context)
        {
        }

        public BinaryEqualExpression(Expression left, Expression right, MethodInfo method, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.Equal, method, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} = {Right}";
        }
    }

    public sealed class BinaryNotEqualExpression : BinaryExpression
    {
        public BinaryNotEqualExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.NotEqual, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} != {Right}";
        }
    }

    public sealed class BinaryGreaterExpression : BinaryExpression
    {
        public BinaryGreaterExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.GreaterThan, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} > {Right}";
        }

    }
    public sealed class BinaryGreaterOrEqualExpression : BinaryExpression
    {
        public BinaryGreaterOrEqualExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.GreaterThanOrEqual, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} >= {Right}";
        }

    }

    public sealed class BinaryLessExpression : BinaryExpression
    {
        public BinaryLessExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.LessThan, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} < {Right}";
        }

    }
    public sealed class BinaryLessOrEqualExpression : BinaryExpression
    {
        public BinaryLessOrEqualExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, DataType.NonNullBool, BinaryOperator.LessThanOrEqual, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} <= {Right}";
        }

    }

    public sealed class BinaryAndExpression : BinaryExpression
    {
        public BinaryAndExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.And, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} and {Right}";
        }

    }

    public sealed class BinaryOrExpression : BinaryExpression
    {
        public BinaryOrExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.Or, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} or {Right}";
        }

    }

    public sealed class BinaryXorExpression : BinaryExpression
    {
        public BinaryXorExpression(Expression left, Expression right, ParserRuleContext context) : base(left, right, BinaryOperator.ExclusiveOr, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} xor {Right}";
        }
    }

    public sealed class BinaryAssignExpression : BinaryExpression
    {
        public BinaryAssignExpression(VariableExpression left, Expression right, ParserRuleContext context )
            : base(left, right, BinaryOperator.Assign, context)
        {
        }

        public override string ToString()
        {
            return $"{Left} <- {Right}";
        }
    }
}
