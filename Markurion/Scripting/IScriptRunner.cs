﻿using System;
using System.Threading.Tasks;

namespace Markurion.Scripting
{
    public interface IScriptRunner
    {
        void Run(string code, IDependencyResolver resolver);

        Action<IDependencyResolver> Compile(string code);
    }
}