// <copyright file="IScriptRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos.Scripting
{
    public interface IScriptRunner
    {
        void Run(string code, IDependencyResolver resolver);

        Action<IDependencyResolver> Compile(string code);
    }
}