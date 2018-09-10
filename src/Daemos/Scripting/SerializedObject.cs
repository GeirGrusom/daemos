// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;

    /// <summary>
    /// Represents a generic dicationary for a ProtoBuf object
    /// </summary>
    [ProtoContract]
    public sealed class SerializedObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedObject"/> class.
        /// </summary>
        public SerializedObject()
        {
            this.Values = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedObject"/> class.
        /// </summary>
        /// <param name="value">Object to read values from</param>
        public SerializedObject(object value)
        {
            this.Type = value.GetType();
            this.Values = new Dictionary<string, string>();

            var props = this.Type.GetProperties().Where(x => x.CanRead).OrderBy(x => x.Name, StringComparer.Ordinal);

            foreach (var prop in props)
            {
                this.Values.Add(prop.Name, prop.GetValue(value).ToString());
            }
        }

        /// <summary>
        /// Gets or sets the type serialized
        /// </summary>
        [ProtoMember(1)]
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the values serialized
        /// </summary>
        [ProtoMember(2)]
        public Dictionary<string, string> Values { get; set; }
    }
}
