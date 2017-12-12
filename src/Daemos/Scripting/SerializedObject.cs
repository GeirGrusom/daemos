// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using ProtoBuf;

    [ProtoContract]
    public sealed class SerializedObject
    {
        [ProtoMember(1)]
        public Type Type { get; set; }

        [ProtoMember(2)]
        public Dictionary<string, string> Values { get; set; }

        public SerializedObject()
        {
            this.Values = new Dictionary<string, string>();
        }

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
    }
}
