using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static Xunit.Assert;
namespace Markurion.Tests
{
    public class DefaultDependencyResolverTests
    {
        public sealed class Instance : IDisposable
        {
            public void Dispose()
            {

            }
        }

        public sealed class TypeWithDependency
        {
            public IDisposable Dependency { get; }
            public TypeWithDependency(IDisposable dependency)
            {
                Dependency = dependency;
            }
        }

        [Fact]
        public void RegisterInstance_ReturnsInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            var value = new Instance();
            resolver.Register(value);

            // Act
            var result = resolver.GetService<Instance>();

            // Assert
            Equal(value, result);
        }

        [Fact]
        public void RegisterInstance_ByInterface_ReturnsInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            var value = new Instance();
            resolver.Register<IDisposable>(value);

            // Act
            var result = resolver.GetService<IDisposable>();

            // Assert
            Equal(value, result);
        }

        [Fact]
        public void RegisterFactory_ReturnsNewInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable>(ir => new Instance());

            // Act
            var result = resolver.GetService<IDisposable>();

            // Assert
            NotNull(result);
        }

        [Fact]
        public void RegisterType_ReturnsNewInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable, Instance>();

            // Act
            var result = resolver.GetService<IDisposable>();

            // Assert
            NotNull(result);
        }

        [Fact]
        public void RegisterType_WithDependency_Resolves()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable, Instance>();

            // Act
            var result = resolver.GetService<TypeWithDependency>();

            // Assert
            NotNull(result.Dependency);
        }

        [Fact]
        public void CreateProxy_RetrievesBaseTypeIfNotFound()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable, Instance>();
            var proxy = resolver.CreateProxy();

            // Act
            var result = proxy.GetService<IDisposable>();

            // Arrange
            NotNull(result);
        }
    }
}
