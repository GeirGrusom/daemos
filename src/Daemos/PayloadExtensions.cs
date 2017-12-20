// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class contains helper functions for payload queries
    /// </summary>
    public static class PayloadExtensions
    {
        /// <summary>
        /// Indicates if a payload contains a member with a given value or not
        /// </summary>
        /// <param name="payload">Payload to test</param>
        /// <param name="memberName">Member name to test</param>
        /// <param name="value">Value to test</param>
        /// <returns>Returns true if the value is present</returns>
        public static object PayloadContains(this IDictionary<string, object> payload, string memberName, object value)
        {
            return Equals(payload[memberName], value);
        }

        /// <summary>
        /// Gets the payload member value
        /// </summary>
        /// <param name="payload">Payload to read</param>
        /// <param name="memberName">Member to read</param>
        /// <returns>Value of the payload member</returns>
        public static object GetPayloadMember(this IDictionary<string, object> payload, string memberName)
        {
            if (payload.ContainsKey(memberName))
            {
                return payload[memberName];
            }

            return float.NaN; // Not equal to anything including itself.
        }

        /// <summary>
        /// Compares a payload
        /// </summary>
        /// <param name="payload">Payload to compare</param>
        /// <param name="memberName">Member to compare</param>
        /// <param name="comparer">Comparator</param>
        /// <returns>True the payload compares equal</returns>
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
