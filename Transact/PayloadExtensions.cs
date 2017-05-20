using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact
{
    public static class PayloadExtensions
    {
        public static object PayloadContains(this IDictionary<string, object> payload, string memberName, object value)
        {
            return Equals(payload[memberName], value);
        }

        public static object GetPayloadMember(this IDictionary<string, object> payload, string memberName)
        {
            if(payload.ContainsKey(memberName))
            {
                return payload[memberName];
            }
            return float.NaN; // Not equal to anything including itself.
        }

        public static bool PayloadCompare(this IDictionary<string, object> payload, string memberName, Predicate<dynamic> comparer)
        {
            dynamic value;

            if(payload.TryGetValue(memberName, out value))
            {
                return comparer(value);
            }

            return false;            
        }
    }
}
