// <copyright file="MuteTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Mute.Compilation;
    using NSubstitute;
    using Scripting;
    using Xunit;

    // This warning is disabled in order to keep declared element close to their usage.
#pragma warning disable SA1201 // Elements must appear in the correct order
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
                    return this.transaction;
                }

                set
                {
                    this.transaction = value;
                    this.DependencyResolver.GetService<Transaction>().Returns(this.transaction);
                }
            }

            public Service()
            {
                this.Compiler = new Compiler();
                this.StateSerializer = Substitute.For<IStateSerializer>();
                this.StateDeserializer = Substitute.For<IStateDeserializer>();
                this.DependencyResolver = Substitute.For<IDependencyResolver>();
                this.Storage = Substitute.For<ITransactionStorage>();
                this.Storage.CommitTransactionDeltaAsync(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => Task.FromResult(x.ArgAt<Transaction>(1)));
                this.Storage.CommitTransactionDelta(Arg.Any<Transaction>(), Arg.Any<Transaction>()).Returns(x => x.ArgAt<Transaction>(1));
                this.Transaction = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionStatus.Initialized, null, null, this.Storage);
            }

            public int CompileAndRun(string code)
            {
                var result = this.Compiler.Compile(code);
                Assert.True(result.Success, string.Join("\r\n", result.Messages.Select(x => x.Message)));

                return result.Result(this.StateSerializer, this.StateDeserializer, this.DependencyResolver);
            }

            public T CompileWithResult<T>(string expression)
            {
                var result = default(T);
                this.StateSerializer.When(x => x.Serialize<T>("result", Arg.Any<T>())).Do(x => result = x.ArgAt<T>(1));
                var compilationResult = this.Compiler.Compile($"module foo; let result <- ${expression}; await commit this;");
                Assert.True(compilationResult.Success, string.Join("\r\n", compilationResult.Messages.Select(x => x.Message)));
                compilationResult.Result(this.StateSerializer, this.StateDeserializer, this.DependencyResolver);
                this.StateSerializer.Received().Serialize<T>("result", Arg.Any<T>());
                return result;
            }

            public object CompileWithResult(string expression, Type resultType)
            {
                var method = this.GetType().GetMethod("CompileWithResult", new[] { typeof(string) });

                var genericMethod = method.MakeGenericMethod(resultType);

                try
                {
                    return genericMethod.Invoke(this, new object[] { expression });
                }
                catch (TargetInvocationException ex)
                {
                    var dif = ExceptionDispatchInfo.Capture(ex.InnerException);
                    dif.Throw();
                    return null;
                }
            }

            public List<Mute.CompilationMessage> CompileWithErrors(string code)
            {
                var result = this.Compiler.Compile(code);
                Assert.False(result.Success);

                return result.Messages;
            }
        }

        [Fact]
        public void Compile_Success()
        {
            // Arrange
            var muteCompiler = new Compiler();
            var result = muteCompiler.Compile("module foo;\r\nlet s <- import string;");

            var serializer = new StateSerializer();
            var deserializer = new StateDeserializer();
            var dependencyResolver = new DefaultDependencyResolver();
            dependencyResolver.Register<Transaction>(TransactionFactory.CreateNew(null), null);
            dependencyResolver.Register<ITimeService>(Substitute.For<ITimeService>(), null);
            dependencyResolver.Register("Hello World!", null);

            // Act
            var state = result.Result(serializer, deserializer, dependencyResolver);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public void Compile_Greeting_Success()
        {
            // Arrange
            var muteCompiler = new Compiler();
            var result = muteCompiler.Compile("module foo;\r\nlet s <- import 'greeting' as string;");

            var serializer = new StateSerializer();
            var deserializer = new StateDeserializer();
            var dependencyResolver = new DefaultDependencyResolver();
            dependencyResolver.Register<Transaction>(TransactionFactory.CreateNew(null), null);
            dependencyResolver.Register<ITimeService>(Substitute.For<ITimeService>(), null);
            dependencyResolver.Register("Hello World!", "greeting");

            // Act
            var state = result.Result(serializer, deserializer, dependencyResolver);

            // Assert
            Assert.True(result.Success);
        }

        public static readonly object[][] ResultData =
        {
            new object[] { "@2017-01-01Z", new DateTime(2017, 01, 01, 0, 0, 0, DateTimeKind.Utc) },
            new object[] { "@2017-01-01+02:00", new DateTime(2016, 12, 31, 22, 0, 0, DateTimeKind.Utc) },
            new object[] { "timespan(hours: 2, minutes: 4, seconds: 8)", new TimeSpan(2, 4, 8) },
            new object[] { "@2017-01-01Z + timespan(hours: 2, minutes: 4, seconds: 8)", new DateTime(2017, 01, 01, 2, 4, 8, DateTimeKind.Utc) }
        };

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
        [InlineData("float!(10)", 10.0)]
        [MemberData(nameof(ResultData))]
        public void Expression_CorrectResult(string expression, object result)
        {
            // Arrange
            var service = new Service();

            // Act
            object res = service.CompileWithResult(expression, result.GetType());

            // Assert
            Assert.Equal(result, res);
        }

        [Fact]
        public void Expression_Cast10ToDouble_ReturnsDouble()
        {
            // Arrange
            var service = new Service();

            // Act
            object res = service.CompileWithResult<double>("float!(10)");

            // Assert
            Assert.Equal(10.0, res);
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
            Assert.Equal("Foo", (await storage.FetchTransactionAsync(trans.Id)).Script);
        }

        [Fact]
        public async Task Await_InCatch_Ok()
        {
            // Arrange
            var service = new Service();
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
            Assert.Equal(2, (await storage.FetchTransactionAsync(trans.Id)).Revision);
        }

        [Fact]
        public void With_AltersTransactionScript()
        {
            // Arrange
            var service = new Service();
            var t = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, null, null, TransactionStatus.Initialized, null, null, null);

            // Act
            var result = service.CompileWithResult<Transaction>("this with { Script: \"Foo\" }");

            // Assert
            Assert.Equal("Foo", result.Script);
        }

        [Fact]
        public void With_AltersPayload()
        {
            // Arrange
            var service = new Service();
            dynamic payload = new ExpandoObject();
            payload.Foo = "Bar";

            var t = new Transaction(Guid.NewGuid(), 1, DateTime.UtcNow, null, null, payload, null, TransactionStatus.Initialized, null, null, null);

            // Act
            var result = service.CompileWithResult<Transaction>("this with { Payload: { Status: 'OK' } }");

            // Assert
            Assert.Equal(((dynamic)result.Payload).Status, "OK");
        }

        [Fact]
        public void Expression_OneGreaterOrEqual()
        {
            var service = new Service();

            bool res = service.CompileWithResult<bool>("1 >= 1");

            Assert.True(res);
        }

        public class Foobar
        {
            public int GetFoo(int? a = 1)
            {
                return a.GetValueOrDefault();
            }

            public string GetString()
            {
                return "Hello World!";
            }
        }

        [Fact]
        public void FunctionCall_OptionalParameter()
        {
            var service = new Service();
            int result = 0;
            service.StateSerializer.When(x => x.Serialize<int>("result", Arg.Any<int>())).Do(x => result = x.ArgAt<int>(1));
            service.Compiler.ImplicitImports.Add("foobar", typeof(Foobar));
            service.DependencyResolver.GetService<Foobar>().Returns(new Foobar());

            service.CompileAndRun("module foo; let foo <- import foobar; let result <- foo.GetFoo(); await commit this;");

            Assert.Equal(1, result);
        }

        [Fact]
        public void FunctionCall_ReturnsString()
        {
            var service = new Service();
            string result = null;
            service.StateSerializer.When(x => x.Serialize<string>("result", Arg.Any<string>())).Do(x => result = x.ArgAt<string>(1));
            service.Compiler.ImplicitImports.Add("foobar", typeof(Foobar));
            service.DependencyResolver.GetService<Foobar>().Returns(new Foobar());

            service.CompileAndRun("module foo; let foo <- import foobar; let result <- foo.GetString(); await commit this;");

            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void Constructor_NamedArguments_Ok()
        {
            var service = new Service();

            var ts = service.CompileWithResult<TimeSpan>("timespan(hours: 1, minutes: 2, seconds: 3)");

            Assert.Equal(new TimeSpan(1, 2, 3), ts);
        }

        [Fact]
        public void Constructor_Arguments_Ok()
        {
            var service = new Service();

            var ts = service.CompileWithResult<TimeSpan>("timespan(1, 2, 3)");

            Assert.Equal(new TimeSpan(1, 2, 3), ts);
        }

        [Fact]
        public void Expression_LessOrEqual()
        {
            var service = new Service();

            bool res = service.CompileWithResult<bool>("1 <= 1");

            Assert.True(res);
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
            Assert.Equal("The expression '100' is not nullable.", errors[0].Message);
        }

        [Fact]
        public void NotNull_StringVariableExpressionIsNull_ThrowsNullReferenceException()
        {
            // Arrange
            var service = new Service();

            // Act
            // Assert
            var exception = Assert.Throws<NullReferenceException>(() => service.CompileAndRun("module foo; let a <- string?!(null); let b <- !!a;"));
        }

        public interface IExceptionReporter
        {
            void Report(Exception ex);

            void ThrowException();
        }

        [Fact]
        public void Try_CatchesInvalidOperationException()
        {
            var service = new Service();
            var reporter = Substitute.For<IExceptionReporter>();
            reporter.When(r => r.ThrowException()).Throw(new InvalidOperationException());
            service.DependencyResolver.GetService<IExceptionReporter>().Returns(reporter);
            service.Compiler.ImplicitImports.Add(nameof(IExceptionReporter), typeof(IExceptionReporter));
            service.Compiler.ImplicitImports.Add(nameof(InvalidOperationException), typeof(InvalidOperationException));

            service.CompileAndRun(@"
module foo;

let reporter <- import IExceptionReporter;

try 
{
  reporter.ThrowException();
}
catch<InvalidOperationException>
{
  reporter.Report(exception);
}

");

            reporter.Received().Report(Arg.Any<InvalidOperationException>());
        }

        public interface IRetryReporter
        {
            void Report(int count);

            void Throw();
        }

        [Fact]
        public void Retry_RetriesOnce()
        {
            var service = new Service();
            var reporter = Substitute.For<IRetryReporter>();
            reporter.When(r => r.Throw()).Throw<Exception>();
            service.DependencyResolver.GetService<IRetryReporter>().Returns(reporter);
            service.Compiler.ImplicitImports.Add(nameof(IRetryReporter), typeof(IRetryReporter));
            service.Compiler.ImplicitImports.Add(nameof(Exception), typeof(Exception));

            service.CompileAndRun(@"
module foo;

let reporter <- import IRetryReporter;
var count <- 0;

try 
{
  if(count < 1)
  {
    reporter.Throw();
  }
}
catch<Exception>
{
  count <- count + 1;
  retry;
}
reporter.Report(count);
");

            reporter.Received().Report(1);
        }

        [Fact]
        public void NotNull_ExpressionIsNull_ThrowsNullReferenceException()
        {
            // Arrange
            var service = new Service();

            // Act
            // Assert
            var exception = Assert.Throws<NullReferenceException>(() => service.CompileAndRun("module foo; let a <- !!string?!(null);"));
        }
    }
}
