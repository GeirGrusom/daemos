using Markurion.Mute.Interperator;
using System;
using System.Collections.Generic;
using System.Text;
using Markurion.Mute.Expressions;
using Xunit;
using Antlr4.Runtime;
using Markurion.Scripting;
using NSubstitute;
using static Xunit.Assert;
using System.Threading.Tasks;
using System.Reflection;

namespace Markurion.Tests
{
    using static ExpressionHelper;
    public class ExpressionCompilerTests
    {

        public class Service
        {
            public ExpressionCompiler ExpressionCompiler { get; }
            public IDependencyResolver DependencyResolver { get; }

            public ITransactionStorage Storage { get; }

            public IStateSerializer StateSerializer { get; }
            public IStateDeserializer StateDeserializer { get; }

            public Service()
            {
                ExpressionCompiler = new ExpressionCompiler();
                DependencyResolver = Substitute.For<IDependencyResolver>();
                Storage = Substitute.For<ITransactionStorage>();
                StateSerializer = Substitute.For<IStateSerializer>();
                StateDeserializer = Substitute.For<IStateDeserializer>();
                DependencyResolver.GetService<Transaction>().Returns(TransactionFactory.CreateNew(Storage));
            }

            public T CompileAndRun<T>(Expression expression)
            {
                var func = ExpressionCompiler.PartialCompile<T>(expression);
                return func(StateSerializer, StateDeserializer, DependencyResolver);
            }

            public object CompileAndRun(Expression expression, Type resultType)
            {
                var meth = typeof(ExpressionCompiler).GetMethod(nameof(ExpressionCompiler.PartialCompile), new[] { typeof(Expression) }).MakeGenericMethod(resultType);
                var func = (Delegate)meth.Invoke(ExpressionCompiler, new object[] { expression });
                return func.DynamicInvoke(StateSerializer, StateDeserializer, DependencyResolver);
            }

            public T CompileAndRun<T>(Action<ExpressionCompiler, Expression> action, Expression expression)
            {
                var func = ExpressionCompiler.PartialCompile<T>(exp => { action(ExpressionCompiler, exp); }, expression);
                return func(StateSerializer, StateDeserializer, DependencyResolver);
            }
        }

        
        [Fact]
        public void Compile_With_EmptyObjectExpression()
        {
            // Arrange
            var service = new Service();

            var objectExpression = new ObjectExpression(new ObjectMember[0], ParserRuleContext.EmptyContext);

            var exp =
                new WithExpression(
                    new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext), 
                    new ObjectExpression(new ObjectMember[0], ParserRuleContext.EmptyContext), ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>(exp);

            // Assert
            NotNull(result);
        }

        [Fact]
        public void Compile_With_ObjectExpression_AltersScript()
        {
            // Arrange
            var service = new Service();

            var objectExpression = new ObjectExpression(new ObjectMember[] {
                new ObjectMember("Script", new ConstantExpression(DataType.NonNullString, "Hello", ParserRuleContext.EmptyContext), ParserRuleContext.EmptyContext)
            }, ParserRuleContext.EmptyContext);

            var exp =
                new WithExpression(
                    new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext),
                    objectExpression, ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>(exp);

            // Assert
            NotNull(result);
            Equal("Hello", result.Script);
        }

        [Fact]
        public void Compile_With_ObjectExpression_IncrementsVersion()
        {
            // Arrange
            var service = new Service();

            var objectExpression = new ObjectExpression(new ObjectMember[] {
                new ObjectMember("Script", new ConstantExpression(DataType.NonNullString, "Hello", ParserRuleContext.EmptyContext), ParserRuleContext.EmptyContext)
            }, ParserRuleContext.EmptyContext);

            var exp =
                new WithExpression(
                    new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext),
                    objectExpression, ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>((comp, expression) => comp.OnVisit((WithExpression)expression, true), exp);

            // Assert
            Equal(2, result.Revision);
        }

        [Fact]
        public void Compile_CommitTransaction_IncrementsRevision()
        {
            // Arrange
            var service = new Service();
            service.Storage.CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => x.ArgAt<Transaction>(1));
            var exp = new CommitTransactionExpression(new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext), "commit", false, ParserRuleContext.EmptyContext);

            // Act
            Transaction trans = service.CompileAndRun<Transaction>(exp);

            // Assert
            service.Storage.Received().CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>());
            Equal(2, trans.Revision);
        }


        public static readonly object[][] Expressions =
        {
           new object[] { Add(Constant(1), Constant(1)), 2 },
           new object[] { Mul(Constant(3), Constant(4)), 12},
           new object[] { Div(Constant(3), Constant(4)), 0 }
        };

        [Theory]
        [MemberData(nameof(Expressions))]
        public void Expression_Results(Expression expression, object expectedResult)
        {
            // Arrange
            var service = new Service();
            
            // Act
            object result = service.CompileAndRun(expression, expectedResult.GetType());

            // Assert
            Equal(expectedResult, result);
            
        }
    }
}
