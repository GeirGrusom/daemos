// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    /// <summary>
    /// Specifies how a type is to be serialized, or deserialized
    /// </summary>
    [Flags]
    public enum SerializationFlags : byte
    {
        /// <summary>
        /// The type has no value
        /// </summary>
        Null = 0x00,

        /// <summary>
        /// The type has a value
        /// </summary>
        NotNull = 0x01,

        /// <summary>
        /// The type should serialize using a <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter" />
        /// </summary>
        BinaryFormatter = 0x02 | NotNull,

        /// <summary>
        /// The type should serialize using <see cref="ISerializable"/>
        /// </summary>
        Serializable = 0x04 | NotNull,

        /// <summary>
        /// The type should serialize using ProtoBuf
        /// </summary>
        ProtoBuf = 0x08 | NotNull,
    }
}
