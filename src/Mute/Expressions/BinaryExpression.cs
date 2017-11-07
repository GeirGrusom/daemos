// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Reflection;
    using Antlr4.Runtime;

    /// <summary>
    /// Lists all binary operators
    /// </summary>
    public enum BinaryOperator
    {
        /// <summary>
        /// Node is an add expression
        /// </summary>
        Add,

        /// <summary>
        /// Node is a subtract expression
        /// </summary>
        Subtract,

        /// <summary>
        /// Node is a multiply expression
        /// </summary>
        Multiply,

        /// <summary>
        /// Node is a divide expression
        /// </summary>
        Divide,

        /// <summary>
        /// Node is a remainder expression
        /// </summary>
        Remainder,

        /// <summary>
        /// Node is an equal expression
        /// </summary>
        Equal,

        /// <summary>
        /// Node is a not equal expression
        /// </summary>
        NotEqual,

        /// <summary>
        /// Node is a greater than expression
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Node is a less than or equal expression
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Node is a less than expression
        /// </summary>
        LessThan,

        /// <summary>
        /// Node is a less than or equal expression
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Node is a logical and expression
        /// </summary>
        And,

        /// <summary>
        /// Node is a logical or expression
        /// </summary>
        Or,

        /// <summary>
        /// Node is a logical exclusive or expression
        /// </summary>
        ExclusiveOr,

        /// <summary>
        /// Node is an assignment expression
        /// </summary>
        Assign,

        /// <summary>
        /// Node is a with expression
        /// </summary>
        With
    }

    /// <summary>
    /// This class is the super class of all binary operators (operators with exactly two arguments)
    /// </summary>
    public class BinaryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="operator">Operator used by this binary expressionb</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        protected BinaryExpression(Expression left, Expression right, BinaryOperator @operator, ParserRuleContext context)
            : this(left, right, @operator, null, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="operator">Operator used by this binary expression</param>
        /// <param name="method">Method used when this operator is invoked</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        protected BinaryExpression(Expression left, Expression right, BinaryOperator @operator, MethodInfo method, ParserRuleContext context)
            : base(left.Type, context)
        {
            this.Method = method;
            this.Operator = @operator;
            this.Left = left;
            this.Right = right;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="resultType">Operator result data type</param>
        /// <param name="operator">Operator used by this binary expression</param>
        /// <param name="method">Method used when this operator is invoked</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        protected BinaryExpression(Expression left, Expression right, DataType resultType, BinaryOperator @operator, MethodInfo method, ParserRuleContext context)
            : base(resultType, context)
        {
            this.Method = method;
            this.Operator = @operator;
            this.Left = left;
            this.Right = right;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryExpression"/> class.
        /// </summary>
        /// <param name="left">Left hand expression</param>
        /// <param name="right">Right hand expression</param>
        /// <param name="resultType">Operator result data type</param>
        /// <param name="operator">Operator used by this binary expression</param>
        /// <param name="context">Parser rule context this expression was made from</param>
        protected BinaryExpression(Expression left, Expression right, DataType resultType, BinaryOperator @operator, ParserRuleContext context)
            : this(left, right, resultType, @operator, null, context)
        {
            this.Method = null;
            this.Operator = @operator;
            this.Left = left;
            this.Right = right;
        }

        /// <summary>
        /// Gets the left hand expression
        /// </summary>
        public Expression Left { get; }

        /// <summary>
        /// Gets the right hand expression
        /// </summary>
        public Expression Right { get; }

        /// <summary>
        /// Gets the operator type
        /// </summary>
        public BinaryOperator Operator { get; }

#pragma warning disable SA1134 // Attributes must not share line
        /// <summary>
        /// Gets the method
        /// </summary>
        /// <remarks>
        /// This value can be null
        /// </remarks>
        public MethodInfo Method { [return: CanBeNull]get; }
#pragma warning restore SA1134 // Attributes must not share line
    }
}
