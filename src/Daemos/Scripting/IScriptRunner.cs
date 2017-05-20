using System;

namespace Daemos.Scripting
{
    public interface IScriptRunner
    {
        void Run(string code, IDependencyResolver resolver);

        Action<IDependencyResolver> Compile(string code);
    }
}