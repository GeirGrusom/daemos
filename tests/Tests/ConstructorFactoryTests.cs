using System;
using NSubstitute;
using Xunit;

namespace Daemos.Tests
{
    public class ConstructorFactoryTests
    {
        public class Service
        {
            public ConstructorFactory Factory { get; }
            public IDependencyResolver DependencyResolver { get; }

            public Service()
            {
                Factory = new ConstructorFactory();
                DependencyResolver = Substitute.For<IDependencyResolver>();
            }

            public T Create<T>()
                where T : class
            {
                var ctor = Factory.Create<T>();
                return ctor(DependencyResolver);
            }
        }

        public class ClassWithDefaultConstructor
        {
        }

        public class ClassWithSingleArgument<TArgument>
        {
            public TArgument Argument { get; }
            public ClassWithSingleArgument(TArgument argument)
            {
                Argument = argument;
            }
        }

        [Fact]
        public void Create_ClassWithPublicDefaultConstrucotr_ReturnsInstance()
        {
            // Arrange
            var service = new Service();

            // Act
            var result = service.Create<ClassWithDefaultConstructor>();

            // Assert
            Assert.NotNull(result);
        }

        public class ClassWithDefaultParameters
        {
            public int Parameter { get; }

            public ClassWithDefaultParameters(int parameter = 1)
            {
                Parameter = parameter;
            }
        }

        [Fact]
        public void Create_ClassWithDefaultArgument_ReturnsInstance()
        {
            // Arrange
            var service = new Service();

            // Act
            var result = service.Create<ClassWithDefaultParameters>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Parameter);

        }

        [Fact]
        public void Create_ClassWithSingleArgumentPublicConstrucotr_ReturnsInstance()
        {
            // Arrange
            var service = new Service();
            service.DependencyResolver.GetService<string>().Returns("Foo");

            // Act
            var result = service.Create<ClassWithSingleArgument<string>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Foo", result.Argument);
        }

        [Fact]
        public void Create_ClassWithSingleArgument_ResolutionFails_ThrowsDependencyResolutionExcepton()
        {
            // Arrange
            var service = new Service();

            // Act
            var result = Assert.Throws<DependencyFailedException>(() => service.Create<ClassWithSingleArgument<Uri>>());

            // Assert
            Assert.Equal(typeof(Uri), result.Type);
        }
    }
}
