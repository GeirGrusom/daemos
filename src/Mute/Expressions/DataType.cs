// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public struct DataType : IEquatable<DataType>
    {
        public Type ClrType { get; }

        public bool Nullable { get; }

        public static readonly Dictionary<Type, DataType> ClrTypeMap = new Dictionary<Type, DataType>();

        public static readonly HashSet<DataType> Types = new HashSet<DataType>();

        public static DataType NonNullBool { get; } = new DataType(typeof(bool), false);

        public static DataType NonNullString { get; } = new DataType(typeof(string), false);

        public static DataType NonNullInt { get; } = new DataType(typeof(int), false);

        public static DataType NonNullLong { get; } = new DataType(typeof(long), false);

        public static DataType NonNullFloat { get; } = new DataType(typeof(float), false);

        public static DataType NonNullDouble { get; } = new DataType(typeof(double), false);

        public static DataType NonNullDecimal { get; } = new DataType(typeof(decimal), false);

        public static DataType NonNullDateTime { get; } = new DataType(typeof(DateTime), false);

        public static DataType NonNullDateTimeOffset { get; } = new DataType(typeof(DateTimeOffset), false);

        public static DataType NonNullTimeSpan { get; } = new DataType(typeof(TimeSpan), false);

        public static DataType NonNullTransactionState { get; } = new DataType(typeof(TransactionStatus), false);

        public static DataType NullBool { get; } = new DataType(typeof(bool), true);

        public static DataType NullString { get; } = new DataType(typeof(string), true);

        public static DataType NullInt { get; } = new DataType(typeof(int?), true);

        public static DataType NullLong { get; } = new DataType(typeof(long?), true);

        public static DataType NullFloat { get; } = new DataType(typeof(float?), true);

        public static DataType NullDouble { get; } = new DataType(typeof(double?), true);

        public static DataType NullDecimal { get; } = new DataType(typeof(decimal?), true);

        public static DataType NullDateTime { get; } = new DataType(typeof(DateTime?), true);

        public static DataType NullDateTimeOffset { get; } = new DataType(typeof(DateTimeOffset?), true);

        public static DataType NullTimeSpan { get; } = new DataType(typeof(TimeSpan?), true);

        public static DataType Dynamic { get; } = new DataType(typeof(IDictionary<string, object>), true);

        public static DataType Void { get; } = new DataType(typeof(void), false);

        public string Name => this.ClrType.Name;

        public static DataType FromClrType(Type type)
        {
            bool isNullable = !type.GetTypeInfo().IsValueType || type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            return new DataType(type, isNullable);
        }

        private static bool GetNullable(ParameterInfo parameter)
        {
            foreach (var item in parameter.CustomAttributes)
            {
                var name = item.AttributeType.Name;
                if (name == "NotNullAttribute")
                {
                    return false;
                }
                if (name == "CanBeNullAttribute")
                {
                    return true;
                }
            }
            var type = parameter.ParameterType;
            return !type.GetTypeInfo().IsValueType || type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static DataType FromMethodInfoReturnType(MethodInfo meth)
        {
            return FromParameter(meth.ReturnParameter);
        }

        public static DataType FromParameter(ParameterInfo parameter)
        {
            return new DataType(parameter.ParameterType, GetNullable(parameter));
        }

        public DataType(Type clrType, bool nullable)
        {
            this.ClrType = clrType;
            this.Nullable = nullable;
        }

        public override int GetHashCode()
        {
            return this.ClrType.GetHashCode() ^ this.Nullable.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is DataType)
            {
                return this.Equals((DataType) other);
            }
            return false;
        }

        public bool Equals(DataType other)
        {
            return other.Nullable == this.Nullable && other.ClrType == this.ClrType;
        }

        public static bool operator ==(DataType lhs, DataType rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DataType lhs, DataType rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override string ToString()
        {
            if (this.Nullable) return this.ClrType.Name + "?";
            return this.ClrType.Name;
        }
    }
}
