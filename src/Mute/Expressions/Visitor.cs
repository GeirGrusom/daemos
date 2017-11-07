// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using System.Diagnostics;

    public class Visitor
    {
        [DebuggerStepThrough]
        public void Visit(Expression expression)
        {
            this.OnVisit(expression);
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(BinaryExpression exp)
        {
            if (exp is BinaryAddExpression)
                this.OnVisit((BinaryAddExpression)exp);
            else if (exp is BinarySubtractExpression)
                this.OnVisit((BinarySubtractExpression)exp);
            else if (exp is BinaryMultiplyExpression)
                this.OnVisit((BinaryMultiplyExpression)exp);
            else if (exp is BinaryDivideExpression)
                this.OnVisit((BinaryDivideExpression)exp);
            else if (exp is BinaryEqualExpression)
                this.OnVisit((BinaryEqualExpression)exp);
            else if (exp is BinaryNotEqualExpression)
                this.OnVisit((BinaryNotEqualExpression)exp);
            else if (exp is BinaryAssignExpression)
                this.OnVisit((BinaryAssignExpression)exp);
            else if (exp is BinaryGreaterExpression)
                this.OnVisit((BinaryGreaterExpression)exp);
            else if (exp is BinaryGreaterOrEqualExpression)
                this.OnVisit((BinaryGreaterOrEqualExpression)exp);
            else if (exp is BinaryLessExpression)
                this.OnVisit((BinaryLessExpression)exp);
            else if (exp is BinaryLessOrEqualExpression)
                this.OnVisit((BinaryLessOrEqualExpression)exp);
            else if (exp is BinaryAndExpression)
                this.OnVisit((BinaryAndExpression)exp);
            else if (exp is BinaryOrExpression)
                this.OnVisit((BinaryOrExpression)exp);
            else if (exp is BinaryXorExpression)
                this.OnVisit((BinaryXorExpression)exp);
            else if (exp is WithExpression)
                this.OnVisit((WithExpression)exp);
            else
                throw new NotImplementedException($"The binary operator '{exp.GetType().Name}' is not implemented.");
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(UnaryExpression exp)
        {
            if (exp is UnarySubtractExpression unarySubtractExpression)
                this.OnVisit(unarySubtractExpression);
            else if (exp is UnaryAwaitExpression unaryAwaitExpression)
                this.OnVisit(unaryAwaitExpression);
            else if (exp is UnaryNotExpression unaryNotExpression)
                this.OnVisit(unaryNotExpression);
            else if (exp is UnaryConvertExpression unaryConvertExpression)
                this.OnVisit(unaryConvertExpression);
            else if (exp is NotNullExpression notNullExpression)
                this.OnVisit(notNullExpression);
            else
                throw new NotImplementedException($"The unary type '{exp.GetType().Name}' is not implemented.");
        }

        public virtual void OnVisit(BinaryAndExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryOrExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryXorExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(TryExpression exp)
        {
            this.OnVisit(exp.Body);
            foreach (var c in exp.CatchExpressions)
            {
                this.OnVisit(c);
            }
            this.OnVisit(exp.Finally);
        }

        public virtual void OnVisit(CallExpression exp)
        {
            this.OnVisit(exp.Instance);
            foreach (var arg in exp.Arguments)
            {
                this.OnVisit(arg);
            }
        }

        public virtual void OnVisit(MemberExpression exp)
        {
            this.OnVisit(exp.Instance);
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(Expression exp)
        {
            if (exp == null)
            {
                return;
            }

            if (exp is BinaryExpression)
                this.OnVisit((BinaryExpression)exp);
            else if (exp is UnaryExpression)
                this.OnVisit((UnaryExpression)exp);
            else if (exp is ModuleExpression)
                this.OnVisit((ModuleExpression)exp);
            else if (exp is ConstantExpression)
                this.OnVisit((ConstantExpression)exp);
            else if (exp is VariableExpression)
                this.OnVisit((VariableExpression)exp);
            else if (exp is BlockExpression)
                this.OnVisit((BlockExpression)exp);
            else if (exp is VariableDeclarationExpression)
                this.OnVisit((VariableDeclarationExpression)exp);
            else if (exp is CallExpression)
                this.OnVisit((CallExpression)exp);
            else if (exp is RetryExpression)
                this.OnVisit((RetryExpression)exp);
            else if (exp is TryExpression)
                this.OnVisit((TryExpression)exp);
            else if (exp is CatchExpression)
                this.OnVisit((CatchExpression)exp);
            else if (exp is ConditionalExpression)
                this.OnVisit((ConditionalExpression)exp);
            else if (exp is ImportExpression)
                this.OnVisit((ImportExpression)exp);
            else if (exp is CommitTransactionExpression)
                this.OnVisit((CommitTransactionExpression)exp);
            else if (exp is ObjectExpression)
                this.OnVisit((ObjectExpression)exp);
            else if (exp is NamedArgument)
                this.OnVisit((NamedArgument)exp);
            else if (exp is MemberExpression)
                this.OnVisit((MemberExpression)exp);
            else
                throw new NotImplementedException();
        }

        public virtual void OnVisit(NamedArgument exp)
        {
            this.OnVisit(exp.Value);
        }

        public virtual void OnVisit(ObjectExpression exp)
        {
            foreach (var mem in exp.Members)
            {
                this.OnVisit(mem);
            }
        }

        public virtual void OnVisit(ObjectMember exp)
        {
            this.OnVisit(exp.Value);
        }

        public virtual void OnVisit(CommitTransactionExpression exp)
        {
            this.OnVisit(exp.Transaction);
        }

        public virtual void OnVisit(RetryExpression retry)
        {
        }

        public virtual void OnVisit(BinaryAddExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(NotNullExpression exp)
        {
            this.OnVisit(exp.Operand);
        }

        public virtual void OnVisit(ConditionalExpression exp)
        {
            this.OnVisit(exp.Condition);
            this.OnVisit(exp.IfValue);
            if (exp.ElseValue != null)
            {
                this.OnVisit(exp.ElseValue);
            }
        }

        public virtual void OnVisit(BinarySubtractExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryGreaterExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryGreaterOrEqualExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(CatchExpression exp)
        {
            this.OnVisit(exp.Body);
        }

        public virtual void OnVisit(BinaryLessExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryLessOrEqualExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryMultiplyExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryDivideExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryEqualExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryNotEqualExpression exp)
        {
            this.OnVisit(exp.Left);
            this.OnVisit(exp.Right);
        }

        public virtual void OnVisit(UnaryAddExpression exp)
        {
            this.Visit(exp.Operand);
        }

        public virtual void OnVisit(UnarySubtractExpression exp)
        {
            this.Visit(exp.Operand);
        }

        public virtual void OnVisit(UnaryNotExpression exp)
        {
            this.Visit(exp.Operand);
        }

        public virtual void OnVisit(UnaryAwaitExpression exp)
        {
            this.Visit(exp.Operand);
        }

        public virtual void OnVisit(UnaryConvertExpression exp)
        {
            this.Visit(exp.Operand);
        }

        public virtual void OnVisit(ConstantExpression exp)
        {
        }

        public virtual void OnVisit(ModuleExpression exp)
        {
            this.Visit(exp.Body);
        }

        public virtual void OnVisit(BlockExpression exp)
        {
            foreach (var item in exp.Body)
            {
                this.Visit(item);
            }
        }

        public virtual void OnVisit(VariableExpression exp)
        {
        }

        public virtual void OnVisit(VariableDeclarationExpression exp)
        {
            this.Visit(exp.Variable);
            this.Visit(exp.Assignment);
        }

        public virtual void OnVisit(WhileExpression exp)
        {
            this.Visit(exp.Condition);
            this.Visit(exp.Contents);
        }

        public virtual void OnVisit(BinaryAssignExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
        }

        public virtual void OnVisit(ImportExpression exp)
        {
        }

        public virtual void OnVisit(WithExpression exp)
        {
            this.Visit(exp.Left);
            this.Visit(exp.Right);
        }
    }
}
