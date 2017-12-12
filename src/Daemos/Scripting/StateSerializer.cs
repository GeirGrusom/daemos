// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

#if USE_BINARYFORMATTER
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Daemos.Scripting
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using ProtoBuf;

    /// <summary>
    /// Serializes script state
    /// </summary>
    public sealed class StateSerializer : IDisposable, IStateSerializer
    {
        private static MethodInfo serializeGenericMethod = typeof(StateSerializer).GetMethods().Single(x => x.IsGenericMethodDefinition && x.Name == "Serialize");

        private readonly BinaryWriter writer;
        private readonly MemoryStream memoryStream;
        private readonly GZipStream gzipStream;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateSerializer"/> class.
        /// </summary>
        public StateSerializer()
        {
            this.memoryStream = new MemoryStream();
            this.gzipStream = new GZipStream(this.memoryStream, CompressionMode.Compress);
            this.writer = new BinaryWriter(this.gzipStream, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the underlying stream for this StateSerializer.
        /// </summary>
        public System.IO.Stream UnderlyingStream => this.gzipStream;

        /// <summary>
        /// Disposes this instance and the underlying streams.
        /// </summary>
        public void Dispose()
        {
            this.writer.Dispose();
            this.gzipStream.Dispose();
            this.memoryStream.Dispose();
            this.disposed = true;
        }

        /// <summary>
        /// Gets the state serialized to this instance.
        /// </summary>
        /// <returns>State serialized as a byte array</returns>
        public byte[] GetState() => this.memoryStream.ToArray();

        /// <summary>
        /// Writes the script stage to the state
        /// </summary>
        /// <param name="stage">Stage to write</param>
        public void WriteStage(int stage)
        {
            this.AssertNotDisposed();
            this.writer.Write(stage);
        }

        /// <summary>
        /// Writes the stage as end of script
        /// </summary>
        public void WriteEndStage()
        {
            this.AssertNotDisposed();
            this.writer.Write(-1);
        }

        /// <summary>
        /// Serializes null for the specified variable of the specified type.
        /// </summary>
        /// <param name="name">Name of the variable to serialize</param>
        /// <param name="type">Type to serialize for</param>
        public void SerializeNull(string name, Type type)
        {
            this.AssertNotDisposed();
            this.writer.Write(JenkinsHash.GetHashCode(name + type.Name));
            this.writer.Write(false);
        }

        /// <summary>
        /// Serializes a value for the specified variable.
        /// </summary>
        /// <param name="name">Name of the variable to serialize.</param>
        /// <param name="value">Value to serialize.</param>
        public void Serialize(string name, object value)
        {
            serializeGenericMethod.MakeGenericMethod(value.GetType()).Invoke(this, new object[] { name, value });
        }

        /// <summary>
        /// Serializes a value for the specified variable.
        /// </summary>
        /// <typeparam name="T">Type of variable to serialize</typeparam>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Value to serialize</param>
        public void Serialize<T>(string name, T value)
        {
            this.AssertNotDisposed();
            this.writer.Write(JenkinsHash.GetHashCode(name + typeof(T).Name));
            if (typeof(T) == typeof(byte))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((byte)(object)value);
                return;
            }

            if (typeof(T) == typeof(char))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((char)(object)value);
                return;
            }

            if (typeof(T) == typeof(bool))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((bool)(object)value);
                return;
            }

            if (typeof(T) == typeof(short))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((short)(object)value);
                return;
            }

            if (typeof(T) == typeof(int))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((int)(object)value);
                return;
            }

            if (typeof(T) == typeof(long))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((long)(object)value);
                return;
            }

            if (typeof(T) == typeof(float))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((float)(object)value);
                return;
            }

            if (typeof(T) == typeof(double))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((double)(object)value);
                return;
            }

            if (typeof(T) == typeof(decimal))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write((decimal)(object)value);
                return;
            }

            if (typeof(T) == typeof(DateTime))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write(((DateTime)(object)value).Ticks);
                return;
            }

            if (typeof(T) == typeof(DateTimeOffset))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write(((DateTimeOffset)(object)value).Ticks);
                this.writer.Write(((DateTimeOffset)(object)value).Offset.Ticks);
                return;
            }

            if (typeof(T) == typeof(string))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                var bytes = Encoding.UTF8.GetBytes((string)(object)value);
                this.writer.Write(bytes.Length);
                this.writer.Write(bytes, 0, bytes.Length);
                return;
            }

            if (typeof(T) == typeof(Type))
            {
                this.writer.Write((byte)SerializationFlags.NotNull);
                this.writer.Write(((Type)(object)value).AssemblyQualifiedName);
                return;
            }

            if (IsSerializable<T>())
            {
                this.Serialize(value);
            }
            else if (IsProtoBuf<T>())
            {
                this.ProtoBuf(value);
            }
            else
            {
                throw new NotSupportedException("The type is not serializable and is not a protobuf contract.");
            }
        }

        internal static bool IsSerializable<T>()
        {
            var interfaces = typeof(T).GetInterfaces();
            var serializableInterface = interfaces.SingleOrDefault(x => x == typeof(ISerializable));
            var ctor = typeof(T).GetConstructor(new[] { typeof(IStateDeserializer) });
            return serializableInterface != null && ctor != null;
        }

        internal static bool IsProtoBuf<T>()
        {
            return typeof(T).GetTypeInfo().GetCustomAttribute<ProtoContractAttribute>() != null;
        }

        private void AssertNotDisposed()
        {
            if (this.disposed)
            {
                throw new InvalidOperationException("The serializer has been disposed.");
            }
        }

        private void Serialize<T>(T value)
        {
            this.writer.Write((byte)SerializationFlags.Serializable);
            var interf = (ISerializable)value;
            interf.Serialize(this);
        }

        private void ProtoBuf<T>(T value)
        {
            this.writer.Write((byte)SerializationFlags.ProtoBuf);
            Serializer.Serialize<T>(this.UnderlyingStream, value);
        }
    }
}
