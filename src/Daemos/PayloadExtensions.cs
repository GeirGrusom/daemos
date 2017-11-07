// <copyright file="PayloadExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Daemos
{
    public static class PayloadExtensions
    {
        public static object PayloadContains(this IDictionary<string, object> payload, string memberName, object value)
        {
            return Equals(payload[memberName], value);
        }

        public static object GetPayloadMember(this IDictionary<string, object> payload, string memberName)
        {
            if (payload.ContainsKey(memberName))
            {
                return payload[memberName];
            }
            return float.NaN; // Not equal to anything including itself.
        }

        public static bool PayloadCompare(this IDictionary<string, object> payload, string memberName, Predicate<dynamic> comparer)
        {
            dynamic value;

            if (payload.TryGetValue(memberName, out value))
            {
                return comparer(value);
            }

            return false;            
        }
    }
}
