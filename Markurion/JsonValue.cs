using System;
using System.Collections.Generic;

namespace Markurion
{
    public struct JsonValue : IComparable<int>, IComparable<float>, IComparable<long>, IComparable<double>, IEquatable<int>, IEquatable<float>, IEquatable<string>, IEquatable<long>, IEquatable<double>, IEquatable<bool>
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
            }
            if(local is long)
            {
                return ((long)local).CompareTo(other);
            }
            if(local is float)
            {
                return ((float)local).CompareTo(other);
            }
            if(local is double)
            {
                return ((double)local).CompareTo(other);
            }
            return -1;
        }

        public int CompareTo(long other)
        {
            object local = GetValue();

            if (local is int)
            {
                return ((long)(int)local).CompareTo(other);
            }
            if (local is long)
            {
                return ((long)local).CompareTo(other);
            }
            if (local is float)
            {
                return ((float)local).CompareTo(other);
            }
            if (local is double)
            {
                return ((double)local).CompareTo(other);
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
            if(local is long)
            {
                return ((long)local).CompareTo((long)other);
            }
            if (local is float)
            {
                return ((float)local).CompareTo(other);
            }
            if(local is double)
            {
                return ((double)local).CompareTo(other);
            }
            return -1;
        }

        public int CompareTo(double other)
        {
            object local = GetValue();

            if (local is int)
            {
                return ((int)local).CompareTo((int)other);
            }
            if (local is long)
            {
                return ((long)local).CompareTo((long)other);
            }
            if (local is float)
            {
                return ((double)(float)local).CompareTo(other);
            }
            if (local is double)
            {
                return ((double)local).CompareTo(other);
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
            if(local is double)
            {
                return (double)local == other;
            }
            if(local is int)
            {
                return (int)local == other;
            }
            if(local is long)
            {
                return (long)local == other;
            }
            return false;
        }

        public bool Equals(double other)
        {
            object local = GetValue();
            if (local is float)
            {
                return (float)local == other;
            }
            if (local is double)
            {
                return (double)local == other;
            }
            if (local is int)
            {
                return (int)local == other;
            }
            if (local is long)
            {
                return (long)local == other;
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
            if(local is double)
            {
                return (double)local == other;
            }
            if (local is int)
            {
                return (int)local == other;
            }
            if (local is long)
            {
                return (long)local == other;
            }

            return false;
        }

        public bool Equals(long other)
        {
            object local = GetValue();
            if (local is float)
            {
                return (float)local == other;
            }
            if (local is double)
            {
                return (double)local == other;
            }
            if (local is int)
            {
                return (int)local == other;
            }
            if (local is long)
            {
                return (long)local == other;
            }

            return false;
        }

        public bool Equals(bool other)
        {
            object local = GetValue();
            if(local is bool)
            {
                return (bool)local == other;
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

        public static bool operator ==(JsonValue json, long value)
        {
            return json.Equals(value);

        }

        public static bool operator !=(JsonValue json, long value)
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

        public static bool operator ==(JsonValue json, double value)
        {
            return json.Equals(value);

        }

        public static bool operator !=(JsonValue json, double value)
        {
            return !json.Equals(value);
        }

        // Int32
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

        // Int64
        public static bool operator >(JsonValue json, long value)
        {
            return json.CompareTo(value) > 0;
        }

        public static bool operator <(JsonValue json, long value)
        {
            return json.CompareTo(value) < 0;
        }

        public static bool operator >=(JsonValue json, long value)
        {
            return json.CompareTo(value) >= 0;
        }

        public static bool operator <=(JsonValue json, long value)
        {
            return json.CompareTo(value) <= 0;
        }

        public static bool Equals(JsonValue lhs, int rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(JsonValue lhs, long rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(JsonValue lhs, float rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(JsonValue lhs, double rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(JsonValue lhs, bool rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(int rhs, JsonValue lhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(long rhs, JsonValue lhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(float rhs, JsonValue lhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(double rhs, JsonValue lhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool Equals(bool rhs, JsonValue lhs)
        {
            return lhs.Equals(rhs);
        }

    }
}
