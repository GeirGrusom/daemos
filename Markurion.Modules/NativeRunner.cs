using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Markurion.Modules
{
    public class NativeRunner : IScriptRunner
    {
        private readonly Dictionary<string, object> _modules;

        public NativeRunner()
        {
            _modules = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        private static readonly object[] EmptyParameters = new object[0];

        public void RegisterModule(string name, object module)
        {
            _modules.Add(name, module);
        }

        public void RegisterModules(IEnumerable<Type> types, Func<Type, object> factory)
        {
            foreach (var type in types)
            {
                var mod = type.GetTypeInfo().GetCustomAttribute<ModuleAttribute>();
                if (mod == null)
                {
                    continue;
                }
                object instance;
                try
                {
                    instance = factory(type);
                }
                catch
                {
                    // Should log!
                    continue;
                }
                RegisterModule(mod.Name ?? type.Name, instance);
            }
        }

        public Task<TransactionMutableData> Run(string code, Transaction transaction)
        {
            int pos = code.IndexOf(':');
            string left = code.Substring(0, pos);
            string right = code.Substring(pos + 1);

            object o = _modules[left];

            var type = o.GetType();

            MethodInfo method = type.GetMethod(right);

            return Task.FromResult((TransactionMutableData)method.Invoke(o, new object[] { transaction }));
        }

        public Task<Func<Transaction, Task<TransactionMutableData>>> Compile(string code)
        {
            throw new NotImplementedException();
        }
    }
}
