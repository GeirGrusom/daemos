using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact
{
    public struct JsonValue : IComparable<int>, IComparable<float>, IEquatable<int>, IEquatable<float>, IEquatable<string>
    {
        private readonly IDictionary<string, object> _owner;
        private readonly string _memberOf;
        private readonly string _member;

        public string MemberOf => _memberOf;
        public string Member => _member;

        public override int GetHashCode()
        {
            return GetValue().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is JsonValue)
            {
                return Equals(GetValue(),((JsonValue)obj).GetValue());
            }
            return Equals(GetValue(), obj);            
        }

        public JsonValue(IDictionary<string, object> owner, string memberOf, string member)
        {
            _owner = owner;
            _memberOf = memberOf;
            _member = member;
        }

        public override string ToString()
        {
            return $"{_memberOf}[{_member}]";
        }

        private object GetValue()
        {
            return _owner[_member];
        }

        public int CompareTo(int other)
        {
            object local = GetValue();

            if(local is int)
            {
                return ((int)local).CompareTo(other);
            } else if(local is float)
            {
                return ((float)local).CompareTo((float)other);
            }
            return -1;
        }

        public int CompareTo(float other)
        {
            object local = GetValue();

            if (local is int)
            {
                return ((int)local).CompareTo((int)other);
            }
            else if (local is float)
            {
                return ((float)local).CompareTo(other);
            }
            return -1;
        }

        public bool Equals(string other)
        {
            object local = GetValue();
            if(local is string)
            {
                return (string)local == other;
            }
            return false;
        }

        public bool Equals(float other)
        {
            object local = GetValue();
            if(local is float)
            {
                return (float)local == other;
            }
            if(local is int)
            {
                return (int)local == other;
            }
            return false;
        }

        public bool Equals(int other)
        {
            object local = GetValue();
            if (local is float)
            {
                return (float)local == other;
            }
            if (local is int)
            {
                return (int)local == other;
            }
            return false;
        }

        public static bool operator ==(JsonValue json, int value)
        {
            return json.Equals(value);

        }

        public static bool operator !=(JsonValue json, int value)
        {
            return !json.Equals(value);
        }

        public static bool operator ==(JsonValue json, string value)
        {
            return json.Equals(value);
        }

        public static bool operator !=(JsonValue json, string value)
        {
            return !json.Equals(value);
        }

        public static bool operator ==(JsonValue json, float value)
        {
            return json.Equals(value);

        }

        public static bool operator !=(JsonValue json, float value)
        {
            return !json.Equals(value);
        }

        public static bool operator >(JsonValue json, int value)
        {
            return json.CompareTo(value) > 0;
        }

        public static bool operator <(JsonValue json, int value)
        {
            return json.CompareTo(value) < 0;
        }

        public static bool operator >=(JsonValue json, int value)
        {
            return json.CompareTo(value) >= 0;
        }

        public static bool operator <=(JsonValue json, int value)
        {
            return json.CompareTo(value) <= 0;
        }

    }
}
