// <copyright file="ExpressionCompilerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System;
    using Antlr4.Runtime;
    using Mute.Compilation;
    using Mute.Expressions;
    using NSubstitute;
    using Scripting;
    using Xunit;

#pragma warning disable SA1201 // Elements must appear in the correct order

    public class ExpressionCompilerTests
    {
        public class Service
        {
            public Service()
            {
                this.ExpressionCompiler = new ExpressionCompiler();
                this.DependencyResolver = Substitute.For<IDependencyResolver>();
                this.Storage = Substitute.For<ITransactionStorage>();
                this.StateSerializer = Substitute.For<IStateSerializer>();
                this.StateDeserializer = Substitute.For<IStateDeserializer>();
                this.DependencyResolver.GetService<Transaction>().Returns(TransactionFactory.CreateNew(this.Storage));
            }

            public ExpressionCompiler ExpressionCompiler { get; }

            public IDependencyResolver DependencyResolver { get; }

            public ITransactionStorage Storage { get; }

            public IStateSerializer StateSerializer { get; }

            public IStateDeserializer StateDeserializer { get; }

            public T CompileAndRun<T>(Expression expression)
            {
                var func = this.ExpressionCompiler.PartialCompile<T>(expression);
                return func(this.StateSerializer, this.StateDeserializer, this.DependencyResolver);
            }

            public object CompileAndRun(Expression expression, Type resultType)
            {
                var meth = typeof(ExpressionCompiler).GetMethod(nameof(this.ExpressionCompiler.PartialCompile), new[] { typeof(Expression) }).MakeGenericMethod(resultType);
                var func = (Delegate)meth.Invoke(this.ExpressionCompiler, new object[] { expression });
                return func.DynamicInvoke(this.StateSerializer, this.StateDeserializer, this.DependencyResolver);
            }

            public T CompileAndRun<T>(Action<ExpressionCompiler, Expression> action, Expression expression)
            {
                var func = this.ExpressionCompiler.PartialCompile<T>(exp => { action(this.ExpressionCompiler, exp); }, expression);
                return func(this.StateSerializer, this.StateDeserializer, this.DependencyResolver);
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
                    new ObjectExpression(new ObjectMember[0], ParserRuleContext.EmptyContext),
                    ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>(exp);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Compile_With_ObjectExpression_AltersScript()
        {
            // Arrange
            var service = new Service();

            var objectExpression = new ObjectExpression(
                new ObjectMember[]
                {
                    new ObjectMember(
                        "Script",
                        new ConstantExpression(DataType.NonNullString, "Hello", ParserRuleContext.EmptyContext),
                        ParserRuleContext.EmptyContext)
                }, ParserRuleContext.EmptyContext);

            var exp =
                new WithExpression(
                    new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext),
                    objectExpression,
                    ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>(exp);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hello", result.Script);
        }

        [Fact]
        public void Compile_With_ObjectExpression_IncrementsVersion()
        {
            // Arrange
            var service = new Service();

            var objectExpression = new ObjectExpression(
                new ObjectMember[]
                {
                    new ObjectMember("Script", new ConstantExpression(DataType.NonNullString, "Hello", ParserRuleContext.EmptyContext), ParserRuleContext.EmptyContext)
                }, ParserRuleContext.EmptyContext);

            var exp =
                new WithExpression(
                    new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext),
                    objectExpression,
                    ParserRuleContext.EmptyContext);

            // Act
            var result = service.CompileAndRun<Transaction>((comp, expression) => comp.OnVisit((WithExpression)expression, true), exp);

            // Assert
            Assert.Equal(2, result.Revision);
        }

        [Fact]
        public void Compile_CommitTransaction_IncrementsRevision()
        {
            // Arrange
            var service = new Service();
            service.Storage.CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => x.ArgAt<Transaction>(1));
            var exp = new CommitTransactionExpression(new VariableExpression("this", false, new DataType(typeof(Transaction), false), ParserRuleContext.EmptyContext), false, ParserRuleContext.EmptyContext);

            // Act
            Transaction trans = service.CompileAndRun<Transaction>(exp);

            // Assert
            service.Storage.Received().CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>());
            Assert.Equal(2, trans.Revision);
        }

        public class Foobar
        {
            public string Foo()
            {
                return "Hello World!";
            }
        }

        [Fact]
        public void Compile_FunctionCall()
        {
            // Arrange
            var service = new Service();
            var foo = new VariableExpression("foo", true, new DataType(typeof(Foobar), false), ParserRuleContext.EmptyContext);
            service.DependencyResolver.GetService<Foobar>().Returns(new Foobar());
            var res = service.CompileAndRun<string>(
                (cmp, exp) =>
                {
                    cmp.RegisterVariableExtern(foo, true, null);
                    cmp.OnVisit(exp);
                },
                new CallExpression(typeof(Foobar).GetMethod(nameof(Foobar.Foo)), foo, new Expression[0], ParserRuleContext.EmptyContext));

            Assert.Equal("Hello World!", res);
        }

        public static readonly object[][] Expressions =
        {
           new object[] { ExpressionHelper.Add(ExpressionHelper.Constant(1), ExpressionHelper.Constant(1)), 2 },
           new object[] { ExpressionHelper.Mul(ExpressionHelper.Constant(3), ExpressionHelper.Constant(4)), 12 },
           new object[] { ExpressionHelper.Div(ExpressionHelper.Constant(3), ExpressionHelper.Constant(4)), 0 }
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
            Assert.Equal(expectedResult, result);
        }
    }
}
