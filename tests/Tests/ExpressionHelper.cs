using Antlr4.Runtime;
using Daemos.Mute.Expressions;

namespace Daemos.Tests
{
    public static class ExpressionHelper
    {
        public static BinaryAddExpression Add(Expression lhs, Expression rhs)
        {
            return new BinaryAddExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryMultiplyExpression Mul(Expression lhs, Expression rhs)
        {
            return new BinaryMultiplyExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryDivideExpression Div(Expression lhs, Expression rhs)
        {
            return new BinaryDivideExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryEqualExpression Equals(Expression lhs, Expression rhs)
        {
            return new BinaryEqualExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryGreaterExpression Greater(Expression lhs, Expression rhs)
        {
            return new BinaryGreaterExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryGreaterOrEqualExpression GreaterOrEqual(Expression lhs, Expression rhs)
        {
            return new BinaryGreaterOrEqualExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }

        public static BinaryLessExpression Less(Expression lhs, Expression rhs)
        {
            return new BinaryLessExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }
        public static BinaryLessOrEqualExpression LessOrEqual(Expression lhs, Expression rhs)
        {
            return new BinaryLessOrEqualExpression(lhs, rhs, ParserRuleContext.EmptyContext);
        }
        public static ConstantExpression Constant(object value)
        {
            return new ConstantExpression(value, ParserRuleContext.EmptyContext);
        }

        public static ConstantExpression Constant(DataType type, object value)
        {
            return new ConstantExpression(value, ParserRuleContext.EmptyContext);
        }
    }
}
