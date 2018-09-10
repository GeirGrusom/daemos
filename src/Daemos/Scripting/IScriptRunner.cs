// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    /// <summary>
    /// Defines an interface for script runners
    /// </summary>
    public interface IScriptRunner
    {
        /// <summary>
        /// Runs the script specified in code
        /// </summary>
        /// <param name="code">Script code to run</param>
        /// <param name="resolver">Dependency resolver</param>
        void Run(string code, IDependencyResolver resolver);

        /// <summary>
        /// Compiles the code specfied
        /// </summary>
        /// <param name="code">Code to compile</param>
        /// <returns>Returns a compiled executable delegate</returns>
        Action<IDependencyResolver> Compile(string code);
    }
}