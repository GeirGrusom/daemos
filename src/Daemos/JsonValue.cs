// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a JSON value in a query
    /// </summary>
    public struct JsonValue : IComparable<int>, IComparable<float>, IComparable<long>, IComparable<double>, IEquatable<int>, IEquatable<float>, IEquatable<string>, IEquatable<long>, IEquatable<double>, IEquatable<bool>
    {
        private readonly IDictionary<string, object> owner;
        private readonly string memberOf;
        private readonly string member;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValue"/> struct.
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="memberOf">Member object</param>
        /// <param name="member">Object member</param>
        public JsonValue(IDictionary<string, object> owner, string memberOf, string member)
        {
            this.owner = owner;
            this.memberOf = memberOf;
            this.member = member;
        }

        /// <summary>
        /// Gets the object this is a member of
        /// </summary>
        public string MemberOf => this.memberOf;

        /// <summary>
        /// Gets the member this is a value for
        /// </summary>
        public string Member => this.member;

        // These operators doesn't require documentation.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Compares a JSON value equal to a string
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">String to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, string rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a JSON value equal to a int
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">Int to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, int rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a JSON value equal to a long
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">Long to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, long rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a JSON value equal to a float
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">Float to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, float rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a JSON value equal to a double
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">Double to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, double rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a JSON value equal to a bool
        /// </summary>
        /// <param name="lhs">JsonValue to compare</param>
        /// <param name="rhs">Bool to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(JsonValue lhs, bool rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares a string equal to a JSON value
        /// </summary>
        /// <param name="lhs">String to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(string lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <summary>
        /// Compares a int equal to a JSON value
        /// </summary>
        /// <param name="lhs">Int to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(int lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <summary>
        /// Compares a long equal to a JSON value
        /// </summary>
        /// <param name="lhs">Long to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(long lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <summary>
        /// Compares a float equal to a JSON value
        /// </summary>
        /// <param name="lhs">Float to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(float lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <summary>
        /// Compares a double equal to a JSON value
        /// </summary>
        /// <param name="lhs">Double to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(double lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <summary>
        /// Compares a bool equal to a JSON value
        /// </summary>
        /// <param name="lhs">Bool to compare</param>
        /// <param name="rhs">JSON value to compare to</param>
        /// <returns>True if they are considered equal otherwise false</returns>
        public static bool Equals(bool lhs, JsonValue rhs)
        {
            return rhs.Equals(lhs);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.GetValue().GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is JsonValue)
            {
                return Equals(this.GetValue(), ((JsonValue)obj).GetValue());
            }

            return Equals(this.GetValue(), obj);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.memberOf}[{this.member}]";
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool Equals(string other)
        {
            object local = this.GetValue();
            if (local is string)
            {
                return (string)local == other;
            }

            return false;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool Equals(bool other)
        {
            object local = this.GetValue();
            if (local is bool)
            {
                return (bool)local == other;
            }

            return false;
        }

        /// <summary>
        /// Gets the JSON value
        /// </summary>
        /// <returns>Returns the value</returns>
        private object GetValue()
        {
            if (!this.owner.TryGetValue(this.member, out var result))
            {
                return float.NaN;
            }

            return result;
        }
    }
}
