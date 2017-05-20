using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Markurion.Mute
{
    using Interperator;
    using Scripting;
    public sealed class MuteScriptRunner : IScriptRunner
    {
        private readonly Compiler _compiler;

        public void AddImplicitType(string typeAlias, Type type)
        {
            _compiler.ImplicitImports.Add(typeAlias, type);
        }

        public void RegisterType(Type type)
        {
            _compiler.NamespaceLookup.RegisterType(type);
        }

        public void RegisterType(Type type, string typeAlias)
        {
            _compiler.NamespaceLookup.RegisterType(type, typeAlias);
        }

        public MuteScriptRunner()
        {
            _compiler = new Compiler();
        }
        public Action<IDependencyResolver> Compile(string code)
        {
            var result = _compiler.Compile(code);

            return r => result.Result(r.GetService<IStateSerializer>(), r.GetService<IStateDeserializer>(), r);
        }

        public void Run(string code, IDependencyResolver resolver)
        {
            var result = _compiler.Compile(code);

            result.Result(resolver.GetService<IStateSerializer>(), resolver.GetService<IStateDeserializer>(), resolver);

        }
    }
}
