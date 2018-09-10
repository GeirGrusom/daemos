// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ProtoBuf;

    /// <summary>
    /// This class represents a serialized expression with ProtoBuf support
    /// </summary>
    [ProtoContract]
    public class SerializedException
    {
        private static readonly HashSet<PropertyInfo> ExceptionProperties = new HashSet<PropertyInfo>(typeof(Exception).GetProperties(BindingFlags.Instance | BindingFlags.Public));

        /// <summary>
        /// Gets or sets the exception type
        /// </summary>
        [ProtoMember(1, IsRequired = true)]
        public Type ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception message
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the stacktrace
        /// </summary>
        [ProtoMember(3, IsRequired =true)]
        public string StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the source
        /// </summary>
        [ProtoMember(4, IsRequired = true)]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets additional properties
        /// </summary>
        [ProtoMember(5, IsRequired = true)]
        public Dictionary<string, string> AdditionalProperties { get; set; }

        /// <summary>
        /// Gets or sets any innerexception
        /// </summary>
        [ProtoMember(6, IsRequired = false)]
        public SerializedException InnerException { get; set; }

        /// <summary>
        /// Creates a serialized exception from a normal exception
        /// </summary>
        /// <param name="ex">Exception to create a serialized exception for</param>
        /// <returns>The resulting serialized exception</returns>
        public static SerializedException FromException(Exception ex)
        {
            var additionalProperties = new Dictionary<string, string>();
            var propertySet = new HashSet<PropertyInfo>(ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance));

            propertySet.ExceptWith(ExceptionProperties);

            foreach (var prop in propertySet)
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
