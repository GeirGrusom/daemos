using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using ProtoBuf;
#if USE_BINARYFORMATTER
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Daemos.Scripting
{

    public sealed class StateSerializer : IDisposable, IStateSerializer
    {
        private readonly BinaryWriter writer;
        private readonly MemoryStream memoryStream;
        private readonly GZipStream gzipStream;
        private bool _disposed;

        public Stream UnderlyingStream => gzipStream;

        public void Dispose()
        {
            writer.Dispose();
            gzipStream.Dispose();
            memoryStream.Dispose();
            _disposed = true;
        }

        public byte[] GetState() => memoryStream.ToArray();

        public StateSerializer()
        {
            memoryStream = new MemoryStream();
            gzipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            writer = new BinaryWriter(gzipStream, Encoding.UTF8);
        }

        private void AssertNotDisposed()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("The serializer has been disposed.");
            }
        }

        public void WriteStage(int stage)
        {
            AssertNotDisposed();
            writer.Write(stage);
        }

        public void WriteEndStage()
        {
            AssertNotDisposed();
            writer.Write(-1);
        }

        public void SerializeNull(string name, Type type)
        {
            AssertNotDisposed();
            writer.Write(JenkinsHash.GetHashCode(name + type.Name));
            writer.Write(false);
        }

        private static MethodInfo SerializeGenericMethod = typeof(StateSerializer).GetMethods().Single(x => x.IsGenericMethodDefinition && x.Name == "Serialize");

        public void Serialize(string name, object value)
        {
            SerializeGenericMethod.MakeGenericMethod(value.GetType()).Invoke(this, new object[] { name, value });
        }

        public void Serialize<T>(string name, T value)
        {
            AssertNotDisposed();
            writer.Write(JenkinsHash.GetHashCode(name + typeof(T).Name));
            if(typeof(T) == typeof(byte))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((byte)(object)value);
                return;
            }
            if(typeof(T) == typeof(char))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((char)(object)value);
                return;
            }
            if(typeof(T) == typeof(bool))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((bool)(object)value);
                return;
            }
            if (typeof(T) == typeof(Int16))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((short)(object)value);
                return;
            }
            if (typeof(T) == typeof(Int32))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((int)(object)value);
                return;
            }
            if(typeof(T) == typeof(Int64))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((long)(object)value);
                return;
            }
            if(typeof(T) == typeof(float))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((float)(object)value);
                return;
            }
            if(typeof(T) == typeof(double))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((double)(object)value);
                return;
            }
            if(typeof(T) == typeof(decimal))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write((decimal)(object)value);
                return;
            }
            if(typeof(T) == typeof(DateTime))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write(((DateTime)(object)value).Ticks);
                return;
            }
            if(typeof(T) == typeof(DateTimeOffset))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write(((DateTimeOffset)(object)value).Ticks);
                writer.Write(((DateTimeOffset)(object)value).Offset.Ticks);
                return;
            }
            if(typeof(T) == typeof(string))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                var bytes = Encoding.UTF8.GetBytes((string)(object)value);
                writer.Write(bytes.Length);
                writer.Write(bytes, 0, bytes.Length);
                return;
            }
            if(typeof(T) == typeof(Type))
            {
                writer.Write((byte)SerializationFlags.NotNull);
                writer.Write(((Type)(object)value).AssemblyQualifiedName);
                return;
            }

            if (IsSerializable<T>())
            {
                Serialize(value);
            }
            else if(IsProtoBuf<T>())
            {
                ProtoBuf(value);
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

        private void Serialize<T>(T value)
        {
            writer.Write((byte)SerializationFlags.Serializable);
            var interf = (ISerializable)value;
            interf.Serialize(this);
        }

        private void ProtoBuf<T>(T value)
        {
            writer.Write((byte)SerializationFlags.ProtoBuf);
            Serializer.Serialize(UnderlyingStream, value);
        }
    }
}
