// <copyright file="DefaultDependencyResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;

    /// <summary>
    /// Implements a default dependency resolver
    /// </summary>
    public sealed class DefaultDependencyResolver : IContainer
    {
        private readonly ConstructorFactory constructorFactory;

        private readonly ConcurrentDictionary<FactoryIndex, Func<IDependencyResolver, object>> factories;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDependencyResolver"/> class.
        /// </summary>
        public DefaultDependencyResolver()
           : this(NullDependencyResolver.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDependencyResolver"/> class with the specified base resolver.
        /// </summary>
        /// <param name="baseResolver">Specifies a resolver to default to if resolution fails.</param>
        public DefaultDependencyResolver(IDependencyResolver baseResolver)
        {
            this.BaseResolver = baseResolver;
            this.constructorFactory = new ConstructorFactory();
            this.factories = new ConcurrentDictionary<FactoryIndex, Func<IDependencyResolver, object>>();
        }

        /// <summary>
        /// Gets the base resolver.
        /// </summary>
        public IDependencyResolver BaseResolver { get; }

        /// <summary>
        /// Registers an instance of type T with the specified name
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="instance">Instane to register for the specified type</param>
        /// <param name="name">Name to use for the registration</param>
        public void Register<T>(T instance, string name)
            where T : class
        {
            this.factories.TryAdd(FactoryIndex.Create<T>(name), ir => instance);
        }

        /// <summary>
        /// Registers a type factory for the specified name and type
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="factory">Factory method to use when the type is resolved</param>
        /// <param name="name">Name of the registration</param>
        public void Register<T>(Func<IDependencyResolver, T> factory, string name)
            where T : class
        {
            this.factories.TryAdd(FactoryIndex.Create<T>(name), factory);
        }

        /// <summary>
        /// Creates a proxy container. A proxy container is a mirror of this instance, but additional type registrations will not
        /// be registered in the container it is a proxy for.
        /// </summary>
        /// <returns>Proxy container using this instance as a base resolver</returns>
        public IContainer CreateProxy()
        {
            return new DefaultDependencyResolver(this);
        }

        /// <summary>
        /// Registers a type using a specified type as target
        /// </summary>
        /// <typeparam name="TFor">Type to register for</typeparam>
        /// <typeparam name="TTo">Type to register to</typeparam>
        /// <param name="name">Name of the registration</param>
        public void Register<TFor, TTo>(string name = null)
            where TFor : class
            where TTo : class
        {
            var fac = this.constructorFactory.Create<TTo>();
            this.factories.TryAdd(FactoryIndex.Create<TFor>(name), fac);
        }

        /// <summary>
        /// Resolves an instance for the specified type
        /// </summary>
        /// <typeparam name="T">Type to register for</typeparam>
        /// <param name="name">Name of the registration</param>
        /// <returns>Returns an instance of an object assignable to T</returns>
        public T GetService<T>(string name)
            where T : class
        {
            return (T)this.GetService(typeof(T), name);
        }

        /// <summary>
        /// Resolves an instance for the specified type
        /// </summary>
        /// <param name="serviceType">Type to resolve</param>
        /// <param name="name">Name of the registration</param>
        /// <returns>Returns an instance of an object assignable to serviceType</returns>
        public object GetService(Type serviceType, string name)
        {
            var index = new FactoryIndex(serviceType, name);
            if (this.factories.TryGetValue(index, out Func<IDependencyResolver, object> factory))
            {
                return factory(this);
            }

            var baseResult = this.BaseResolver.GetService(serviceType);
            if (baseResult != null)
            {
                return baseResult;
            }

            if (serviceType.IsClass && serviceType.GetConstructors().Length != 0)
            {
                var meth = this.constructorFactory.GetType().GetMethod(nameof(ConstructorFactory.Create));
                var factoryMethod = meth.MakeGenericMethod(serviceType);
                var resultFactory = (Func<IDependencyResolver, object>)factoryMethod.Invoke(this.constructorFactory, null);
                if (this.factories.TryAdd(index, resultFactory))
                {
                    return resultFactory(this);
                }
            }

            throw new NotSupportedException();
        }

        private struct FactoryIndex : IEquatable<FactoryIndex>
        {
            public FactoryIndex(Type type, string name)
            {
                this.Type = type;
                this.Name = name;
            }

            public FactoryIndex(Type type)
                : this(type, null)
            {
            }

            public Type Type { get; }

            public string Name { get; }

            public static FactoryIndex Create<T>()
            {
                return new FactoryIndex(typeof(T));
            }

            public static FactoryIndex Create<T>(string name)
            {
                return new FactoryIndex(typeof(T), name);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((this.Type != null ? this.Type.GetHashCode() : 0) * 397) ^ (this.Name != null ? this.Name.GetHashCode() : 0);
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }

                return obj is FactoryIndex && this.Equals((FactoryIndex)obj);
            }

            public bool Equals(FactoryIndex other)
            {
                return this.Type == other.Type && string.Equals(this.Name, other.Name);
            }
        }

        private sealed class NullDependencyResolver : IDependencyResolver
        {
            public static NullDependencyResolver Instance { get; } = new NullDependencyResolver();

            public T GetService<T>(string name)
                where T : class
            {
                return default;
            }

            public object GetService(Type type, string name)
            {
                return null;
            }
        }
    }
}
