// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This class deserializes transaction state from a byte array
    /// </summary>
    public sealed class StateDeserializer : IDisposable, IStateDeserializer
    {
        private static readonly MethodInfo DeserializeMethod = typeof(StateDeserializer).GetMethods().Single(x => x.IsGenericMethodDefinition && x.Name == "Deserialize");

        private readonly MemoryStream memoryStream;
        private readonly GZipStream gzipStream;
        private readonly BinaryReader reader;
        private readonly System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateDeserializer"/> class with the specified transaction state as a byte array.
        /// </summary>
        /// <param name="source">The byte array containing the transaction state</param>
        public StateDeserializer(byte[] source)
        {
            this.memoryStream = new MemoryStream(source, writable: false);
            this.gzipStream = new GZipStream(this.memoryStream, CompressionMode.Decompress);
            this.reader = new BinaryReader(this.gzipStream);
            this.binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateDeserializer"/> class using an empty state.
        /// </summary>
        public StateDeserializer()
            : this(new byte[0])
        {
        }

        /// <summary>
        /// Gets the underlying stream
        /// </summary>
        public Stream UnderlyingStream => this.gzipStream;

        /// <summary>
        /// Disposes the Deserializer and the underlying streams
        /// </summary>
        public void Dispose()
        {
            this.reader.Dispose();
            this.gzipStream.Dispose();
            this.memoryStream.Dispose();
        }

        /// <summary>
        /// Reads the transaction stage from the state.
        /// </summary>
        /// <returns>The stage where the transaction should continue</returns>
        public int ReadStage()
        {
            if (this.memoryStream.Length == 0)
            {
                return 0;
            }

            return this.reader.ReadInt32();
        }

        /// <summary>
        /// Deserializes a variable with the specified name using the expected datatype.
        /// </summary>
        /// <param name="name">The name of the variable</param>
        /// <param name="expectedType">The expected type of the variable</param>
        /// <returns>The value of the variable using the expected type</returns>
        public object Deserialize(string name, Type expectedType)
        {
            return DeserializeMethod.MakeGenericMethod(expectedType).Invoke(this, new object[] { name });
        }

        /// <summary>
        /// Deserializes a variable with the given name to the specified type
        /// </summary>
        /// <typeparam name="T">Type to deserialize as</typeparam>
        /// <param name="name">Variable name</param>
        /// <returns>Value of T</returns>
        public T Deserialize<T>(string name)
        {
            this.CheckFieldName(name + typeof(T).Name);
            SerializationFlags flags = (SerializationFlags)this.reader.ReadByte();
            if (flags == SerializationFlags.Null)
            {
                return (T)(object)null;
            }

            if (flags == SerializationFlags.BinaryFormatter)
            {
                return (T)this.binaryFormatter.Deserialize(this.UnderlyingStream);
            }

            if (flags == SerializationFlags.Serializable)
            {
                var ctor = typeof(T).GetConstructor(new[] { typeof(IStateDeserializer) });
                if (ctor == null)
                {
                    throw new InvalidOperationException("The type implements ISerializable but does not contain a proper constructor. Serializable types requires a public constructor with IStateDeserializer argument.");
                }

                return (T)ctor.Invoke(new object[] { this });
            }

            if (flags == SerializationFlags.ProtoBuf)
            {
                return ProtoBuf.Serializer.Deserialize<T>(this.UnderlyingStream);
            }

            if (typeof(T) == typeof(Type))
            {
                return (T)(object)Type.GetType(this.reader.ReadString(), true);
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                return (T)(object)this.reader.ReadBoolean();
            }

            if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
            {
                return (T)(object)this.reader.ReadByte();
            }

            if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
            {
                return (T)(object)this.reader.ReadChar();
            }

            if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
            {
                return (T)(object)this.reader.ReadInt16();
            }

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                return (T)(object)this.reader.ReadInt32();
            }

            if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
            {
                return (T)(object)this.reader.ReadInt64();
            }

            if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
            {
                return (T)(object)this.reader.ReadSingle();
            }

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                return (T)(object)this.reader.ReadDouble();
            }

            if (typeof(T) == typeof(string))
            {
                int length = this.reader.ReadInt32();
                var data = this.reader.ReadBytes(length);
                return (T)(object)System.Text.Encoding.UTF8.GetString(data);
            }

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                return (T)(object)this.reader.ReadDecimal();
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return (T)(object)new DateTime(this.reader.ReadInt64(), DateTimeKind.Utc);
            }

            if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTime?))
            {
                var ticks = this.reader.ReadInt64();
                var offsetTicks = this.reader.ReadInt64();
                return (T)(object)new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
            }

            throw new NotSupportedException($"The type '{typeof(T).Name}' is not serializable.");
        }

        private void CheckFieldName(string name)
        {
            int computedHash = JenkinsHash.GetHashCode(name);
            int hash = this.reader.ReadInt32();
            if (computedHash != hash)
            {
                throw new InvalidOperationException("The field hash is not what was expected...");
            }
        }
    }
}
