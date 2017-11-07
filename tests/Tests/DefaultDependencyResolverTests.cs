// <copyright file="DefaultDependencyResolverTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable SA1201 // Elements must appear in the correct order

namespace Daemos.Tests
{
    using System;
    using Xunit;

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
            public TypeWithDependency(IDisposable dependency)
            {
                this.Dependency = dependency;
            }

            public IDisposable Dependency { get; }
        }

        [Fact]
        public void RegisterInstance_ReturnsInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            var value = new Instance();
            resolver.Register(value, null);

            // Act
            var result = resolver.GetService<Instance>(null);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void RegisterInstance_ByInterface_ReturnsInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            var value = new Instance();
            resolver.Register<IDisposable>(value, null);

            // Act
            var result = resolver.GetService<IDisposable>(null);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void RegisterFactory_ReturnsNewInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable>(ir => new Instance(), null);

            // Act
            var result = resolver.GetService<IDisposable>(null);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RegisterType_ReturnsNewInstance()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable, Instance>();

            // Act
            var result = resolver.GetService<IDisposable>(null);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void RegisterType_WithDependency_Resolves()
        {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposable, Instance>();

            // Act
            var result = resolver.GetService<TypeWithDependency>(null);

            // Assert
            Assert.NotNull(result.Dependency);
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
            Assert.NotNull(result);
        }
    }
}
