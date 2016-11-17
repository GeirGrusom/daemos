using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Transact
{
    public class TransactionHandlerFactory : ITransactionHandlerFactory
    {
        private readonly Dictionary<string, ITransactionHandler> _handlers;
        private readonly Func<Type, ITransactionHandler> _serviceProvider;

        public TransactionHandlerFactory(Func<Type, ITransactionHandler> factory)
        {
            _handlers = new Dictionary<string, ITransactionHandler>();
            _serviceProvider = factory;
        }

        public void AddAssembly(Assembly assembly)
        {
            var types = assembly.GetExportedTypes();
            foreach (var type in types)
            {
                if (type.GetTypeInfo().IsAbstract)
                    continue;
                TypeEntry? entry = FilterType(type);
                if (entry == null)
                    continue;
                _handlers.Add(entry.Value.Name, _serviceProvider(entry.Value.Type));
            }
        }

        private struct TypeEntry
        {
            public TypeEntry(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public Type Type { get; }
        }

        private static TypeEntry? FilterType(Type input)
        {
            var interfaces = input.GetInterfaces();
            if (!interfaces.Contains(typeof (ITransactionHandler)))
                return null;
            var attr = input.GetTypeInfo().GetCustomAttribute<HandlerNameAttribute>();
            var name = attr?.Name ?? input.Name;

            return new TypeEntry(name, input);
        }

        public ITransactionHandler Get(string name)
        {
            ITransactionHandler handler;
            if (!_handlers.TryGetValue(name, out handler))
                return null;
            return handler;
        }
    }
}