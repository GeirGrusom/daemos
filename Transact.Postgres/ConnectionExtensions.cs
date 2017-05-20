using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;

using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
using System.Reflection;

namespace Transact.Postgres
{
    public static class ConnectionExtensions
    {

        private static readonly Dictionary<Type, Delegate> _parameterCache = new Dictionary<Type, Delegate>();

        private static void ApplyParameters<T>(NpgsqlCommand cmd, T parameters)
        {
            Action<NpgsqlCommand, T> apply;
            Delegate getValue;
            if(!_parameterCache.TryGetValue(typeof(T), out getValue))
            {
                getValue = CreateApplyParameter<T>();
                _parameterCache.Add(typeof(T), getValue);
            }
            apply = (Action<NpgsqlCommand, T>)getValue;
            apply(cmd, parameters);
        }

        private static readonly Dictionary<Type, NpgsqlDbType> typeDictionary = new Dictionary<Type, NpgsqlDbType>
        {
            [typeof(string)] = NpgsqlDbType.Varchar,
            [typeof(int)] = NpgsqlDbType.Integer,
            [typeof(int?)] = NpgsqlDbType.Integer,
            [typeof(long)] = NpgsqlDbType.Bigint,
            [typeof(short)] = NpgsqlDbType.Smallint,
            [typeof(Guid)] = NpgsqlDbType.Uuid,
            [typeof(Guid?)] = NpgsqlDbType.Uuid,
            [typeof(JsonContainer)] = NpgsqlDbType.Jsonb,
            [typeof(decimal)] = NpgsqlDbType.Money,
            [typeof(DateTime)] = NpgsqlDbType.Timestamp,
            [typeof(DateTime?)] = NpgsqlDbType.Timestamp
        };

        private static NpgsqlDbType GetSqlTypeForType(Type t)
        {
            return typeDictionary[t];
        }

        private static Delegate CreateApplyParameter<T>()
        {
            var type = typeof(T);
            List<Expression> body = new List<Expression>();
            var par = Variable(typeof(NpgsqlParameter), "parameter");
            var cmd = Parameter(typeof(NpgsqlCommand), "cmd");
            var parameters = Parameter(type, "parameters");
            var ctor = typeof(NpgsqlParameter).GetConstructor(new [] { typeof(string), typeof(NpgsqlDbType) });
            var param = typeof(NpgsqlCommand).GetProperty("Parameters", typeof(NpgsqlParameterCollection), new Type[0]);
            var props = Property(cmd, param);
            
            var add = typeof(NpgsqlParameterCollection).GetMethod("Add", new Type[] { typeof(NpgsqlParameter) });
            var toString = typeof(object).GetMethod("ToString");
            foreach(var prop in type.GetProperties())
            {
                var sqlType = GetSqlTypeForType(prop.PropertyType);
                Expression propVal = Property(parameters, prop);

                if (prop.PropertyType == typeof(JsonContainer))
                    propVal = Call(propVal, toString);
                
                body.Add(Assign(par, New(ctor, Constant(prop.Name), Constant(sqlType))));
               
                body.Add(Assign(Property(par, "Value"), Condition(Equal(Convert(propVal, typeof(object)), Constant(null)), Constant(DBNull.Value, typeof(object)),  Convert(propVal, typeof(object)))));
                body.Add(Call(props, add, par));
            }

            var lambda = Lambda<Action<NpgsqlCommand, T>>(Block(new[] { par }, body), cmd, parameters);
            return lambda.Compile();
        }

        public static NpgsqlDataReader ExecuteReader<T>(this NpgsqlConnection conn, string sql, T parameters)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;

                ApplyParameters(cmd, parameters);

                bool failed = false;

                do
                {
                    try
                    {
                        return cmd.ExecuteReader();
                    }
                    catch(InvalidOperationException)
                    {
                        failed = true;
                    }
                }
                while (failed);
            }
            throw new InvalidOperationException();
        }

        public static T ExecuteScalar<T>(this NpgsqlConnection conn, string sql, T parameters)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                ApplyParameters(cmd, parameters);

                object result = cmd.ExecuteScalar();
                if (result == DBNull.Value || result == null)
                    return default(T);

                return (T)result;
            }
        }
    }
}
