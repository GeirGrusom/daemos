// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    /// <summary>
    /// This interface defines how to deserialize from a serialization target
    /// </summary>
    public interface IStateDeserializer : IDisposable
    {
        /// <summary>
        /// Deserializes the named variable with the type T
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="name">Name of variable to deserialize</param>
        /// <returns>Instance of <typeparamref name="T"/></returns>
        T Deserialize<T>(string name);

        /// <summary>
        /// Deserialize the named variable with the type <paramref name="expectedType"/>
        /// </summary>
        /// <param name="name">Name of variable to deserialize</param>
        /// <param name="expectedType">The type that the variable is expected to have</param>
        /// <returns>An instance of <paramref name="expectedType"/></returns>
        object Deserialize(string name, Type expectedType);

        /// <summary>
        /// Reads what stage the serialization target is in. This should be read before anything else.
        /// </summary>
        /// <returns>Stage of serialization target</returns>
        int ReadStage();
    }
}