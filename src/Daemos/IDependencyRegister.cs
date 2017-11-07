// <copyright file="IDependencyRegister.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos
{
    using System;
    public interface IDependencyRegister
    {
        void Register<T>(T instance, string name = null)
            where T : class;

        void Register<T>(Func<IDependencyResolver, T> factory, string name = null)
            where T : class;

        void Register<TFor, TTo>(string name = null)
            where TFor : class
            where TTo : class;
    }
}
