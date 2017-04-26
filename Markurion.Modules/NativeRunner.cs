using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Markurion.Modules
{
    using Scripting;
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

        public void Run(string code, IDependencyResolver resolver)
        {
            int pos = code.IndexOf(':');
            string left = code.Substring(0, pos);
            string right = code.Substring(pos + 1);

            object o = _modules[left];

            var type = o.GetType();

            MethodInfo method = type.GetMethod(right);

            method.Invoke(o, new object[] { resolver });
        }

        public Action<IDependencyResolver> Compile(string code)
        {
            throw new NotImplementedException();
        }
    }
}
