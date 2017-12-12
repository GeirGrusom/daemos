// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;

    public struct JsonValue : IComparable<int>, IComparable<float>, IComparable<long>, IComparable<double>, IEquatable<int>, IEquatable<float>, IEquatable<string>, IEquatable<long>, IEquatable<double>, IEquatable<bool>
    {
        private readonly IDictionary<string, object> owner;
        private readonly string memberOf;
        private readonly string member;

        public string MemberOf => this.memberOf;

        public string Member => this.member;

        public override int GetHashCode()
        {
            return this.GetValue().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is JsonValue)
            {
                return Equals(this.GetValue(), ((JsonValue)obj).GetValue());
            }

            return Equals(this.GetValue(), obj);
        }

        public JsonValue(IDictionary<string, object> owner, string memberOf, string member)
        {
            this.owner = owner;
            this.memberOf = memberOf;
            this.member = member;
        }

        public override string ToString()
        {
            return $"{this.memberOf}[{this.member}]";
        }

        private object GetValue()
        {
            if (!this.owner.TryGetValue(this.member, out var result))
            {
                return float.NaN;
            }

            return result;
        }

        public int CompareTo(int other)
        {
            object local = this.GetValue();

            if (local is int)
            {
                return ((int)local).CompareTo(other);
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

        public int CompareTo(long other)
        {
            object local = this.GetValue();

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
            object local = this.GetValue();

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
                return ((float)local).CompareTo(other);
            }

            if (local is double)
            {
                return ((double)local).CompareTo(other);
            }

            return -1;
        }

        public int CompareTo(double other)
        {
            object local = this.GetValue();

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
            object local = this.GetValue();
            if (local is string)
            {
                return (string)local == other;
            }

            return false;
        }

        public bool Equals(float other)
        {
            object local = this.GetValue();
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

        public bool Equals(double other)
        {
            object local = this.GetValue();
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
            object local = this.GetValue();
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

        public bool Equals(long other)
        {
            object local = this.GetValue();
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
            object local = this.GetValue();
            if (local is bool)
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

        public static bool Equals(JsonValue lhs, string rhs)
        {
            return lhs.Equals(rhs);
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

        public static bool Equals(string rhs, JsonValue lhs)
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
