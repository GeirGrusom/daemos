// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    public interface IScriptRunner
    {
        void Run(string code, IDependencyResolver resolver);

        Action<IDependencyResolver> Compile(string code);
    }
}