using System;
using System.Diagnostics;

namespace Daemos.Mute.Expressions
{
    public class Visitor
    {
        [DebuggerStepThrough]
        public void Visit(Expression expression)
        {
            OnVisit(expression);
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(BinaryExpression exp)
        {
            if (exp is BinaryAddExpression)
                OnVisit((BinaryAddExpression)exp);
            else if (exp is BinarySubtractExpression)
                OnVisit((BinarySubtractExpression)exp);
            else if (exp is BinaryMultiplyExpression)
                OnVisit((BinaryMultiplyExpression)exp);
            else if (exp is BinaryDivideExpression)
                OnVisit((BinaryDivideExpression)exp);
            else if (exp is BinaryEqualExpression)
                OnVisit((BinaryEqualExpression)exp);
            else if (exp is BinaryNotEqualExpression)
                OnVisit((BinaryNotEqualExpression)exp);
            else if (exp is BinaryAssignExpression)
                OnVisit((BinaryAssignExpression)exp);
            else if (exp is BinaryGreaterExpression)
                OnVisit((BinaryGreaterExpression)exp);
            else if (exp is BinaryGreaterOrEqualExpression)
                OnVisit((BinaryGreaterOrEqualExpression)exp);
            else if (exp is BinaryLessExpression)
                OnVisit((BinaryLessExpression)exp);
            else if (exp is BinaryLessOrEqualExpression)
                OnVisit((BinaryLessOrEqualExpression)exp);
            else if (exp is BinaryAndExpression)
                OnVisit((BinaryAndExpression)exp);
            else if (exp is BinaryOrExpression)
                OnVisit((BinaryOrExpression)exp);
            else if (exp is BinaryXorExpression)
                OnVisit((BinaryXorExpression)exp);
            else if (exp is WithExpression)
                OnVisit((WithExpression)exp);
            else
                throw new NotImplementedException($"The binary operator '{exp.GetType().Name}' is not implemented.");
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(UnaryExpression exp)
        {
            if (exp is UnarySubtractExpression unarySubtractExpression)
                OnVisit(unarySubtractExpression);
            else if (exp is UnaryAwaitExpression unaryAwaitExpression)
                OnVisit(unaryAwaitExpression);
            else if (exp is UnaryNotExpression unaryNotExpression)
                OnVisit(unaryNotExpression);
            else if (exp is UnaryConvertExpression unaryConvertExpression)
                OnVisit(unaryConvertExpression);
            else if (exp is NotNullExpression notNullExpression)
                OnVisit(notNullExpression);
            else
                throw new NotImplementedException($"The unary type '{exp.GetType().Name}' is not implemented.");
        }

        public virtual void OnVisit(BinaryAndExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryOrExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryXorExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(TryExpression exp)
        {
            OnVisit(exp.Body);
            foreach (var c in exp.CatchExpressions)
            {
                OnVisit(c);
            }
            OnVisit(exp.Finally);
        }

        public virtual void OnVisit(CallExpression exp)
        {
            OnVisit(exp.Instance);
            foreach (var arg in exp.Arguments)
            {
                OnVisit(arg);
            }
        }

        public virtual void OnVisit(MemberExpression exp)
        {
            OnVisit(exp.Instance);
        }

        [DebuggerStepThrough]
        public virtual void OnVisit(Expression exp)
        {
            if (exp == null)
            {
                return;
            }

            if (exp is BinaryExpression)
                OnVisit((BinaryExpression)exp);
            else if (exp is UnaryExpression)
                OnVisit((UnaryExpression)exp);
            else if (exp is ModuleExpression)
                OnVisit((ModuleExpression)exp);
            else if (exp is ConstantExpression)
                OnVisit((ConstantExpression)exp);
            else if (exp is VariableExpression)
                OnVisit((VariableExpression)exp);
            else if (exp is BlockExpression)
                OnVisit((BlockExpression)exp);
            else if (exp is VariableDeclarationExpression)
                OnVisit((VariableDeclarationExpression)exp);
            else if (exp is CallExpression)
                OnVisit((CallExpression)exp);
            else if (exp is RetryExpression)
                OnVisit((RetryExpression)exp);
            else if (exp is TryExpression)
                OnVisit((TryExpression)exp);
            else if (exp is CatchExpression)
                OnVisit((CatchExpression)exp);
            else if (exp is ConditionalExpression)
                OnVisit((ConditionalExpression)exp);
            else if (exp is ImportExpression)
                OnVisit((ImportExpression)exp);
            else if (exp is CommitTransactionExpression)
                OnVisit((CommitTransactionExpression)exp);
            else if (exp is ObjectExpression)
                OnVisit((ObjectExpression)exp);
            else if (exp is NamedArgument)
                OnVisit((NamedArgument)exp);
            else if (exp is MemberExpression)
                OnVisit((MemberExpression)exp);
            else
                throw new NotImplementedException();
        }

        public virtual void OnVisit(NamedArgument exp)
        {
            OnVisit(exp.Value);
        }

        public virtual void OnVisit(ObjectExpression exp)
        {
            foreach (var mem in exp.Members)
            {
                OnVisit(mem);
            }
        }

        public virtual void OnVisit(ObjectMember exp)
        {
            OnVisit(exp.Value);
        }

        public virtual void OnVisit(CommitTransactionExpression exp)
        {
            OnVisit(exp.Transaction);
        }

        public virtual void OnVisit(RetryExpression retry)
        {
        }

        public virtual void OnVisit(BinaryAddExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(NotNullExpression exp)
        {
            OnVisit(exp.Operand);
        }

        public virtual void OnVisit(ConditionalExpression exp)
        {
            OnVisit(exp.Condition);
            OnVisit(exp.IfValue);
            if (exp.ElseValue != null)
            {
                OnVisit(exp.ElseValue);
            }
        }

        public virtual void OnVisit(BinarySubtractExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryGreaterExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }
        public virtual void OnVisit(BinaryGreaterOrEqualExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(CatchExpression exp)
        {
            OnVisit(exp.Body);
        }

        public virtual void OnVisit(BinaryLessExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }
        public virtual void OnVisit(BinaryLessOrEqualExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryMultiplyExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryDivideExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryEqualExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(BinaryNotEqualExpression exp)
        {
            OnVisit(exp.Left);
            OnVisit(exp.Right);
        }

        public virtual void OnVisit(UnaryAddExpression exp)
        {
            Visit(exp.Operand);
        }
        public virtual void OnVisit(UnarySubtractExpression exp)
        {
            Visit(exp.Operand);
        }
        public virtual void OnVisit(UnaryNotExpression exp)
        {
            Visit(exp.Operand);
        }

        public virtual void OnVisit(UnaryAwaitExpression exp)
        {
            Visit(exp.Operand);
        }

        public virtual void OnVisit(UnaryConvertExpression exp)
        {
            Visit(exp.Operand);
        }

        public virtual void OnVisit(ConstantExpression exp)
        {
        }

        public virtual void OnVisit(ModuleExpression exp)
        {
            Visit(exp.Body);
        }

        public virtual void OnVisit(BlockExpression exp)
        {
            foreach (var item in exp.Body)
            {
                Visit(item);
            }
        }

        public virtual void OnVisit(VariableExpression exp)
        {

        }

        public virtual void OnVisit(VariableDeclarationExpression exp)
        {
            Visit(exp.Variable);
            Visit(exp.Assignment);
        }

        public virtual void OnVisit(WhileExpression exp)
        {
            Visit(exp.Condition);
            Visit(exp.Contents);
        }

        public virtual void OnVisit(BinaryAssignExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
        }

        public virtual void OnVisit(ImportExpression exp)
        {

        }

        public virtual void OnVisit(WithExpression exp)
        {
            Visit(exp.Left);
            Visit(exp.Right);
        }
    }
}
