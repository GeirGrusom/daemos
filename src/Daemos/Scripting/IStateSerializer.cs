// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    /// <summary>
    /// Defines an interface for serializing to an state blob
    /// </summary>
    public interface IStateSerializer : IDisposable
    {
        /// <summary>
        /// Gets the underlying stream for this serializer
        /// </summary>
        System.IO.Stream UnderlyingStream { get; }

        /// <summary>
        /// Gets the serialized state as an <see cref="Array"/>
        /// </summary>
        /// <returns>An byte array with the serialized state</returns>
        byte[] GetState();

        /// <summary>
        /// Serializes a variable named <paramref name="name"/> using the value <paramref name="value"/> and type <typeparamref name="T"/>
        /// </summary>
        /// <remarks><paramref name="name"/> and <typeparamref name="T"/> are sanity checks. I.e. you cannot determine a type afterwards, but trying to deserialize in the wrong place will fail</remarks>
        /// <typeparam name="T">Type to serialize as</typeparam>
        /// <param name="name">Name of the variable</param>
        /// <param name="value">Value to serialize</param>
        void Serialize<T>(string name, T value);

        /// <summary>
        /// Serializes a null value for the specified variable
        /// </summary>
        /// <param name="name">Variable name to serialize</param>
        /// <param name="type">Type to serialize null for</param>
        void SerializeNull(string name, Type type);

        /// <summary>
        /// Writes an indication that this is the last stage of the script
        /// </summary>
        void WriteEndStage();

        /// <summary>
        /// Writes the current stage for the script
        /// </summary>
        /// <param name="stage">Stage to serialize</param>
        void WriteStage(int stage);
    }
}