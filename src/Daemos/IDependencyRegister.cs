// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// This interface specifies requirements for a dependency register.
    /// </summary>
    public interface IDependencyRegister
    {
        /// <summary>
        /// Registers an instance for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to register for.</typeparam>
        /// <param name="instance">Instance to return when resolved.</param>
        /// <param name="name">Optional name of registration.</param>
        void Register<T>(T instance, string name = null)
            where T : class;

        /// <summary>
        /// Registers a factory for the specified type.
        /// </summary>
        /// <typeparam name="T">Type to register for.</typeparam>
        /// <param name="factory">Factory used to resolve the type..</param>
        /// <param name="name">Optional name of registration.</param>
        void Register<T>(Func<IDependencyResolver, T> factory, string name = null)
            where T : class;

        /// <summary>
        /// Registers a type using a template and an implementation.
        /// </summary>
        /// <typeparam name="TFor">The type to register for.</typeparam>
        /// <typeparam name="TTo">The implementation to use for the type.</typeparam>
        /// <param name="name">Optional name of registration.</param>
        void Register<TFor, TTo>(string name = null)
            where TFor : class
            where TTo : class;
    }
}
