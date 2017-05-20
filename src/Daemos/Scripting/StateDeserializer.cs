using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Daemos.Scripting
{
    public sealed class StateDeserializer : IDisposable, IStateDeserializer
    {
        private readonly MemoryStream memoryStream;
        private readonly GZipStream gzipStream;
        private readonly BinaryReader reader;

        public Stream UnderlyingStream => gzipStream;

        public StateDeserializer(byte[] source)
        {
            memoryStream = new MemoryStream(source, writable:false);   
            gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            reader = new BinaryReader(gzipStream);
        }

        public void Dispose()
        {
            reader.Dispose();
            gzipStream.Dispose();
            memoryStream.Dispose();
        }

        public StateDeserializer()
            : this(new byte[0])
        {
        }

        public int ReadStage()
        {
            if (memoryStream.Length == 0)
            {
                return 0;
            }
            return reader.ReadInt32();
        }

        private static readonly MethodInfo DeserializeMethod = typeof(StateDeserializer).GetMethods().Single(x => x.IsGenericMethodDefinition && x.Name == "Deserialize");

        public object Deserialize(string name, Type expectedType)
        {
            return DeserializeMethod.MakeGenericMethod(expectedType).Invoke(this, new object[] { name });
        }

        public T Deserialize<T>(string name)
        {
            CheckFieldName(name + typeof(T).Name);
            SerializationFlags flags = (SerializationFlags)reader.ReadByte();
            if (flags == SerializationFlags.Null)
            {
                return (T) (object) null;
            }

            if(flags == SerializationFlags.BinaryFormatter)
            {
                throw new NotSupportedException();
            }

            if(flags == SerializationFlags.Serializable)
            {
                var ctor = typeof(T).GetConstructor(new[] { typeof(IStateDeserializer) });
                if(ctor == null)
                {
                    throw new InvalidOperationException("The type implements ISerializable but does not contain a proper constructor. Serializable types requires a public constructor with IStateDeserializer argument.");
                }
                return (T)ctor.Invoke(new object[] { this });
            }

            if(flags == SerializationFlags.ProtoBuf)
            {
                return ProtoBuf.Serializer.Deserialize<T>(UnderlyingStream);
            }

            if(typeof(T) == typeof(Type))
            {
                return (T)(object)Type.GetType(reader.ReadString(), true);
            }
            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                return (T)(object)reader.ReadBoolean();
            }
            if(typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
            {
                return (T)(object)reader.ReadByte();
            }
            if(typeof(T) == typeof(char) || typeof(T) == typeof(char?))
            {
                return (T)(object)reader.ReadChar();
            }
            if(typeof(T) == typeof(short) || typeof(T) == typeof(short?))
            {
                return (T)(object)reader.ReadInt16();
            }
            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                return (T)(object)reader.ReadInt32();
            }
            if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
            {
                return (T)(object)reader.ReadInt64();
            }
            if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
            {
                return (T)(object)reader.ReadSingle();
            }
            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                return (T)(object)reader.ReadDouble();
            }
            if (typeof(T) == typeof(string))
            {
                int length = reader.ReadInt32();
                var data = reader.ReadBytes(length);
                return (T)(object)System.Text.Encoding.UTF8.GetString(data);
            }
            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                return (T) (object) reader.ReadDecimal();
            }
            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return (T)(object)new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
            }
            if(typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTime?))
            {
                var ticks = reader.ReadInt64();
                var offsetTicks = reader.ReadInt64();
                return (T)(object)new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
            }
            throw new NotSupportedException($"The type '{typeof(T).Name}' is not serializable.");
        }

        private void CheckFieldName(string name)
        {
            int computedHash = JenkinsHash.GetHashCode(name);
            int hash = reader.ReadInt32();
            if (computedHash != hash)
            {
                throw new InvalidOperationException("The field hash is not what was expected...");
            }
        }
    }
}
