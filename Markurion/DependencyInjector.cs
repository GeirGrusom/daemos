using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Markurion
{
    public interface IDependencyResolver
    {
        T GetService<T>() where T : class;
        T GetService<T>(string name) where T : class;
    }

    public interface IDependencyRegister
    {
        void Register<T>(T instance)
            where T : class;

        void Register<T>(Func<IDependencyResolver, T> factory)
            where T : class;

        void Register<TFor, TTo>()
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
            public T GetService<T>()
                where T : class
            {
                return default(T);
            }

            public T GetService<T>(string name)
                where T : class
            {
                return default(T);
            }
        }

        public IDependencyResolver BaseResolver { get; }

        private readonly ConstructorFactory _constructorFactory;

        private readonly ConcurrentDictionary<Type, Func<IDependencyResolver, object>> _factories;

        public DefaultDependencyResolver()
            : this(NullDependencyResolver.Instance)
        {
        }

        public DefaultDependencyResolver(IDependencyResolver baseResolver)
        {
            BaseResolver = baseResolver;
            _constructorFactory = new ConstructorFactory();
            _factories = new ConcurrentDictionary<Type, Func<IDependencyResolver, object>>();
        }

        public void Register<T>(T instance)
            where T : class
        {
            _factories.TryAdd(typeof(T), ir => instance);
        }

        public void Register<T>(Func<IDependencyResolver, T> factory)
            where T : class
        {
            _factories.TryAdd(typeof(T), factory);
        }

        public IContainer CreateProxy()
        {
            return new DefaultDependencyResolver(this);
        }

        public void Register<TFor, TTo>()
            where TFor : class
            where TTo : class
        {
            var fac = _constructorFactory.Create<TTo>();
            _factories.TryAdd(typeof(TFor), fac);
        }

        public T GetService<T>()
            where T : class
        {
            if(_factories.TryGetValue(typeof(T), out Func<IDependencyResolver, object> factory))
            {
                return (T)factory(this);
            }
            else
            {
                var baseResult = BaseResolver.GetService<T>();
                if (baseResult != null)
                {
                    return baseResult;
                }

                var fac = _constructorFactory.Create<T>();
                _factories.TryAdd(typeof(T), fac);
                return fac(this);
            }
        }

        public T GetService<T>(string name)
            where T : class
        {
            throw new NotImplementedException();
        }
    }
}
