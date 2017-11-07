// <copyright file="SerializedObject.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProtoBuf;

namespace Daemos.Scripting
{
    [ProtoContract]
    public sealed class SerializedObject
    {
        [ProtoMember(1)]
        public Type Type { get; set; }

        [ProtoMember(2)]
        public Dictionary<string, string> Values { get; set; }

        public SerializedObject()
        {
            Values = new Dictionary<string, string>();
        }

        public SerializedObject(object value)
        {
            Type = value.GetType();
            Values = new Dictionary<string, string>();

            var props = Type.GetProperties().Where(x => x.CanRead).OrderBy(x => x.Name, StringComparer.Ordinal);

            foreach (var prop in props)
            {
                Values.Add(prop.Name, prop.GetValue(value).ToString());
            }
        }
    }
}
