// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute
{
    using System;
    using System.Linq;
    using Daemos.Mute.Compilation;
    using Daemos.Scripting;

    /// <summary>
    /// This class represents a runner for MuteScript
    /// </summary>
    public sealed class MuteScriptRunner : IScriptRunner
    {
        private readonly Compiler compiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MuteScriptRunner"/> class.
        /// </summary>
        public MuteScriptRunner()
        {
            this.compiler = new Compiler();
        }

        /// <summary>
        /// Adds an implicit type alias
        /// </summary>
        /// <param name="typeAlias">The name or alias of the type imported. This does not have to be the same as the name of the type.</param>
        /// <param name="type">The type to alias</param>
        public void AddImplicitType(string typeAlias, Type type)
        {
            this.compiler.ImplicitImports.Add(typeAlias, type);
        }

        /// <summary>
        /// Registers an import of the specified type from its namespace
        /// </summary>
        /// <param name="type">Type to register</param>
        public void RegisterType(Type type)
        {
            this.compiler.NamespaceLookup.RegisterType(type);
        }

        /// <summary>
        /// Registers an import of the specified type from its namespace using the specified alias
        /// </summary>
        /// <param name="type">Type to register</param>
        /// <param name="typeAlias">Alias to use</param>
        public void RegisterType(Type type, string typeAlias)
        {
            this.compiler.NamespaceLookup.RegisterType(type, typeAlias);
        }

        /// <summary>
        /// Compiles the specified code and returns a delegate to execute it.
        /// </summary>
        /// <param name="code">Code to compile</param>
        /// <returns>Code entry point delegate</returns>
        public Action<IDependencyResolver> Compile(string code)
        {
            var result = this.compiler.Compile(code);

            return r => result.Result(r.GetService<IStateSerializer>(), r.GetService<IStateDeserializer>(), r);
        }

        /// <summary>
        /// Runs the specified code using the specified dependency resolver.
        /// </summary>
        /// <param name="code">Code to run</param>
        /// <param name="resolver">Dependency resolver used</param>
        public void Run(string code, IDependencyResolver resolver)
        {
            var result = this.compiler.Compile(code);

            if (!result.Success)
            {
                throw new CompilationFailedException("Unable to compile the program.", result.Messages.Select(x => new CompilationError(x.LineNumber, x.Character, x.Message)));
            }

            result.Result(resolver.GetService<IStateSerializer>(), resolver.GetService<IStateDeserializer>(), resolver);
        }
    }
}
