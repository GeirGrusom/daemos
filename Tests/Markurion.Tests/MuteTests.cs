﻿using Markurion.Mute.Interperator;
using Markurion.Scripting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Xunit.Assert;

namespace Markurion.Tests
{
    public class MuteTests
    {

        public class Service
        {
            public Compiler Compiler { get; }

            public IStateSerializer StateSerializer { get; set; }

            public IStateDeserializer StateDeserializer { get; set; }

            public IDependencyResolver DependencyResolver { get; set; }

            public ITransactionStorage Storage { get; set; }

            private Transaction transaction;

            public Transaction Transaction
            {
                get
                {
                    return transaction;
                }
                set
                {
                    transaction = value;
                    DependencyResolver.GetService<Transaction>().Returns(transaction);
                }
            }

            public Service()
            {
                Compiler = new Compiler();
                StateSerializer = Substitute.For<IStateSerializer>();
                StateDeserializer = Substitute.For<IStateDeserializer>();
                DependencyResolver = Substitute.For<IDependencyResolver>();
                Storage = Substitute.For<ITransactionStorage>();
                Storage.CommitTransactionDeltaAsync(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.ArgAt<Transaction>(1)));
                Storage.CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => x.ArgAt<Transaction>(1));
                Transaction = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null, Storage);
            }

            public int CompileAndRun(string code)
            {
                var result = Compiler.Compile(code);
                True(result.Success, string.Join("\r\n", result.Messages.Select(x => x.Message)));

                return result.Result(StateSerializer, StateDeserializer, DependencyResolver);
            }

            public T CompileWithResult<T>(string expression)
            {
                var result = default(T);
                StateSerializer.When(x => x.Serialize<T>("result", Arg.Any<T>())).Do(x => result = x.ArgAt<T>(1));
                var compilationResult = Compiler.Compile($"module foo; let result <- ${expression}; await commit this;");
                True(compilationResult.Success, string.Join("\r\n", compilationResult.Messages.Select(x => x.Message)));
                compilationResult.Result(StateSerializer, StateDeserializer, DependencyResolver);
                StateSerializer.Received().Serialize<T>("result", Arg.Any<T>());
                return result;
            }

            public object CompileWithResult(string expression, Type resultType)
            {
                var method = GetType().GetMethod("CompileWithResult", new[] { typeof(string) });

                var genericMethod = method.MakeGenericMethod(resultType);

                try
                {
                    return genericMethod.Invoke(this, new object[] { expression });
                }
                catch(TargetInvocationException ex)
                {
                    var dif = ExceptionDispatchInfo.Capture(ex.InnerException);
                    dif.Throw();
                    return null;
                }
            }

            public List<Mute.CompilationMessage> CompileWithErrors(string code)
            {
                var result = Compiler.Compile(code);
                False(result.Success);

                return result.Messages;               
            }
        }

        [Fact]
        public void Compile_Success()
        {
            // Arrange
            var muteCompiler = new Mute.Interperator.Compiler();
            var result = muteCompiler.Compile("module foo;\r\nlet s <- import string;");

            var serializer = new StateSerializer();
            var deserializer = new StateDeserializer();
            var dependencyResolver = new DefaultDependencyResolver();
            dependencyResolver.Register<Transaction>(TransactionFactory.CreateNew(null));
            dependencyResolver.Register("Hello World!");

            // Act
            var state = result.Result(serializer, deserializer, dependencyResolver);

            // Assert
            True(result.Success);
            
        }

        [Theory]
        [InlineData("2 + 2", 4)]
        [InlineData("2 * 2", 4)]
        [InlineData("2 + 2 * 3", 8)]
        [InlineData("2 * 2 + 3", 7)]
        [InlineData("true or false", true)]
        [InlineData("true and false", false)]
        [InlineData("true xor false", true)]
        [InlineData("true and true or false", true)]
        [InlineData("true and false or false", false)]
        [InlineData("true and false or true", true)]
        [InlineData("1 = 1", true)]
        [InlineData("1 = 0", false)]
        [InlineData("1 > 1", false)]
        [InlineData("1 > 0", true)]
        [InlineData("1 < 1", false)]
        [InlineData("0 < 1", true)]
        [InlineData("2 >= 1", true)]
        [InlineData("0 >= 1", false)]
        [InlineData("1 >= 1", true)]
        [InlineData("2 <= 1", false)]
        [InlineData("0 <= 1", true)]
        public void Expression_CorrectResult(string expression, object result)
        {
                // Arrange
                var service = new Service();

                // Act
                object res = service.CompileWithResult(expression, result.GetType());

                // Assert
                Equal(result, res);
        }

        [Fact]
        public async Task Commit_WithExpression_CommitsTransaction()
        {
            // Arrange
            var service = new Service();
            var storage = new MemoryStorage();
            var trans = TransactionFactory.CreateNew(storage);
            trans = await storage.CreateTransactionAsync(trans);
            service.Transaction = trans;

            // Act
            service.CompileAndRun(@"module foo;

await commit this with { Script: ""Foo"" };
");

            // Assert
            Equal("Foo", (await storage.FetchTransactionAsync(trans.Id)).Script);
        }

        [Fact]
        public async Task Commit_CommitsTransaction()
        {
            // Arrange
            var service = new Service();
            var storage = new MemoryStorage();
            var trans = TransactionFactory.CreateNew(storage);
            trans = await storage.CreateTransactionAsync(trans);
            service.Transaction = trans;

            // Act
            service.CompileAndRun(@"module foo;

await commit this;
");

            // Assert
            Equal(2, (await storage.FetchTransactionAsync(trans.Id)).Revision);
        }

        [Fact]
        public void With_AltersTransactionScript()
        {
            // Arrange
            var service = new Service();
            var t = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null, null);

            // Act
            var result = service.CompileWithResult<Transaction>("this with { Script: \"Foo\" }");

            // Assert
            Equal("Foo", result.Script);
        }

        [Fact]
        public void Expression_OneGreaterOrEqual()
        {
            var service = new Service();

            bool res = service.CompileWithResult<bool>("1 >= 1");

            True(res);
        }

        [Fact]
        public void DynMeth()
        {

            var d = new DynamicMethod("Test", typeof(bool), null);

            var emit = d.GetILGenerator();

            emit.Emit(OpCodes.Ldc_I4_1);
            emit.Emit(OpCodes.Ldc_I4_1);
            emit.Emit(OpCodes.Cgt);
            emit.Emit(OpCodes.Not);
            emit.Emit(OpCodes.Ret);

            var func = (Func<bool>)d.CreateDelegate(typeof(Func<bool>));


        }

        [Fact]
        public void Expression_LessOrEqual()
        {
            var service = new Service();

            bool res = service.CompileWithResult<bool>("1 <= 1");

            True(res);
        }

        [Fact]
        public void Stores_IntegerVariable()
        {
            // Arrange
            var service = new Service();

            // Act
            service.CompileAndRun("module foo;\r\nlet a <- 100;\r\nawait commit this;");

            // Assert
            service.StateSerializer.Received().Serialize("a", 100);
        }

        [Fact]
        public void Stage1_LoadsVariableValueFromState()
        {
            // Arrange
            var service = new Service();
            service.StateDeserializer.ReadStage().Returns(1); // Move program execution to stage 2
            service.StateDeserializer.Deserialize<int>("a").Returns(200); // Tell runtime that the value of 'a' is actually 200.

            // Act
            service.CompileAndRun("module foo;\r\nlet a <- 100;\r\nawait commit this; let b <- \"Foo\"; await commit this;");

            // Assert
            service.StateSerializer.Received().Serialize("a", 200);
        }

        [Fact]
        public void NotNull_ExpressionIsNotNullable_Fails()
        {
            // Arrange
            var service = new Service();

            // Act
            var errors = service.CompileWithErrors("module foo; let a <- !!100;");

            // Assert
            Equal(errors[0].Message, "The expression '100' is not nullable.");
        }

        [Fact]
        public void NotNull_StringVariableExpressionIsNull_ThrowsNullReferenceException()
        {
            // Arrange
            var service = new Service();

            // Act
            // Assert
            var exception = Throws<NullReferenceException>(() => service.CompileAndRun("module foo; let a <- string?!(null); let b <- !!a;"));
        }

        [Fact]
        public void NotNull_ExpressionIsNull_ThrowsNullReferenceException()
        {
            // Arrange
            var service = new Service();

            // Act
            // Assert
            var exception = Throws<NullReferenceException>(() => service.CompileAndRun("module foo; let a <- !!string?!(null);"));
        }

    }
}
