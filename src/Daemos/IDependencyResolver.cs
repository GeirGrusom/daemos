// <copyright file="IDependencyResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos
{
    using System;

    /// <summary>
    /// This interface describes a type that is able to resolve a dependency
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Gets an instance of an object assignable to T
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        /// <param name="name">Name of registration</param>
        /// <returns>Returns an instance of an object assignable to T</returns>
        T GetService<T>(string name = null)
            where T : class;

        /// <summary>
        /// Gets an instance of an object assignable to T
        /// </summary>
        /// <param name="type">Type to resolve</param>
        /// <param name="name">Name of registration</param>
        /// <returns>Returns an instance of an object assignable to T</returns>
        object GetService(Type type, string name = null);
    }
}
