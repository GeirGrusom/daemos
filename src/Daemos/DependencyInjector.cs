using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Daemos
{
    public interface IDependencyResolver
    {
        T GetService<T>(string name = null) where T : class;
        object GetService(Type type, string name = null);
    }

    public interface IDependencyRegister
    {
        void Register<T>(T instance, string name = null)
            where T : class;

        void Register<T>(Func<IDependencyResolver, T> factory, string name = null)
            where T : class;

        void Register<TFor, TTo>(string name = null)
            where TFor : class
            where TTo : class;
    }

    public interface IContainer : IDependencyResolver, IDependencyRegister
    {
        IContainer CreateProxy();
    }

    public sealed class DefaultDependencyResolver : IContainer
    {

        private sealed class NullDependencyResolver : IDependencyResolver
        {
            public static NullDependencyResolver Instance { get; } = new NullDependencyResolver();

            public T GetService<T>(string name)
                where T : class
            {
                return default(T);
            }

            public object GetService(Type type, string name)
            {
                return null;
            }
        }

        public IDependencyResolver BaseResolver { get; }

        private readonly ConstructorFactory _constructorFactory;

        private readonly ConcurrentDictionary<FactoryIndex, Func<IDependencyResolver, object>> _factories;

        public DefaultDependencyResolver()
            : this(NullDependencyResolver.Instance)
        {
        }

        private struct FactoryIndex : IEquatable<FactoryIndex>
        {
            public Type Type { get; }
            public string Name { get; }

            public FactoryIndex(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public static FactoryIndex Create<T>()
            {
                return new FactoryIndex(typeof(T));
            }

            public static FactoryIndex Create<T>(string name)
            {
                return new FactoryIndex(typeof(T), name);
            }


            public FactoryIndex(Type type)
                : this(type, null)
            {
                
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                return obj is FactoryIndex && Equals((FactoryIndex) obj);
            }

            public bool Equals(FactoryIndex other)
            {
                return Type == other.Type && string.Equals(Name, other.Name);
            }
        }

        public DefaultDependencyResolver(IDependencyResolver baseResolver)
        {
            BaseResolver = baseResolver;
            _constructorFactory = new ConstructorFactory();
            _factories = new ConcurrentDictionary<FactoryIndex, Func<IDependencyResolver, object>>();
        }

        public void Register<T>(T instance, string name)
            where T : class
        {
            _factories.TryAdd(FactoryIndex.Create<T>(name), ir => instance);
        }

        public void Register<T>(Func<IDependencyResolver, T> factory, string name)
            where T : class
        {
            _factories.TryAdd(FactoryIndex.Create<T>(name), factory);
        }

        public IContainer CreateProxy()
        {
            return new DefaultDependencyResolver(this);
        }

        public void Register<TFor, TTo>(string name = null)
            where TFor : class
            where TTo : class
        {
            var fac = _constructorFactory.Create<TTo>();
            _factories.TryAdd(FactoryIndex.Create<TFor>(name), fac);
        }

        public T GetService<T>(string name)
            where T : class
        {
            return (T) GetService(typeof(T), name);
        }

        public object GetService(Type serviceType, string name)
        {
            var index = new FactoryIndex(serviceType, name);
            if (_factories.TryGetValue(index, out Func<IDependencyResolver, object> factory))
            {
                return factory(this);
            }

            var baseResult = BaseResolver.GetService(serviceType);
            if (baseResult != null)
            {
                return baseResult;
            }

            if(serviceType.IsClass && serviceType.GetConstructors().Length != 0)
            {
                var meth = _constructorFactory.GetType().GetMethod(nameof(ConstructorFactory.Create));
                var factoryMethod = meth.MakeGenericMethod(serviceType);
                var resultFactory = (Func<IDependencyResolver, object>) factoryMethod.Invoke(_constructorFactory, null);
                if(_factories.TryAdd(index, resultFactory))
                {
                    return resultFactory(this);
                }
            }

            throw new NotSupportedException();
        }
    }
}
