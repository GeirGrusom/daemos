using System;
using System.Collections.Generic;
using System.Reflection;
using ProtoBuf;

namespace Daemos.Scripting
{
    [ProtoContract]
    public class SerializedException
    {
        [ProtoMember(1, IsRequired = true)]
        public Type ExceptionType { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Message { get; set; }

        [ProtoMember(3, IsRequired =true)]
        public string StackTrace { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public string Source { get; set; }

        [ProtoMember(5, IsRequired = true)]
        public Dictionary<string, string> AdditionalProperties { get; set; }

        [ProtoMember(6, IsRequired = false)]
        public SerializedException InnerException { get; set; }

        private static readonly HashSet<PropertyInfo> ExceptionProperties = new HashSet<PropertyInfo>(typeof(Exception).GetProperties(BindingFlags.Instance | BindingFlags.Public));

        public static SerializedException FromException(Exception ex)
        {
            var additionalProperties = new Dictionary<string, string>();
            var propertySet = new HashSet<PropertyInfo>(ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance));

            propertySet.ExceptWith(ExceptionProperties);

            foreach(var prop in propertySet)
            {
                additionalProperties[prop.Name] = prop.GetValue(ex)?.ToString();
            }

            return new SerializedException
            {
                Message = ex.Message,
                ExceptionType = ex.GetType(),
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException == null ? null : FromException(ex.InnerException),
                AdditionalProperties = additionalProperties,
            };
        }
    }
}
