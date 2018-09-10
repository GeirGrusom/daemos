// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Npgsql;
    using NpgsqlTypes;

    public static class ConnectionExtensions
    {

        private static readonly Dictionary<Type, Delegate> parameterCache = new Dictionary<Type, Delegate>();

        private static void ApplyParameters<T>(NpgsqlCommand cmd, T parameters)
        {
            Action<NpgsqlCommand, T> apply;
            if (!parameterCache.TryGetValue(typeof(T), out Delegate getValue))
            {
                getValue = CreateApplyParameter<T>();
                parameterCache.Add(typeof(T), getValue);
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
            [typeof(DateTime?)] = NpgsqlDbType.Timestamp,
        };

        private static NpgsqlDbType GetSqlTypeForType(Type t)
        {
            return typeDictionary[t];
        }

        private static Delegate CreateApplyParameter<T>()
        {
            var type = typeof(T);
            List<Expression> body = new List<Expression>();
            var par = Expression.Variable(typeof(NpgsqlParameter), "parameter");
            var cmd = Expression.Parameter(typeof(NpgsqlCommand), "cmd");
            var parameters = Expression.Parameter(type, "parameters");
            var ctor = typeof(NpgsqlParameter).GetConstructor(new [] { typeof(string), typeof(NpgsqlDbType) });
            var param = typeof(NpgsqlCommand).GetProperty("Parameters", typeof(NpgsqlParameterCollection), new Type[0]);
            var props = Expression.Property(cmd, param);

            var add = typeof(NpgsqlParameterCollection).GetMethod("Add", new [] { typeof(NpgsqlParameter) });
            var toString = typeof(object).GetMethod("ToString");
            foreach (var prop in type.GetProperties())
            {
                var sqlType = GetSqlTypeForType(prop.PropertyType);
                Expression propVal = Expression.Property(parameters, prop);

                if (prop.PropertyType == typeof(JsonContainer))
                {
                    propVal = Expression.Call(propVal, toString);
                }

                body.Add(Expression.Assign(par, Expression.New(ctor, Expression.Constant(prop.Name), Expression.Constant(sqlType))));

                body.Add(Expression.Assign(Expression.Property(par, "Value"), Expression.Condition(Expression.Equal(Expression.Convert(propVal, typeof(object)), Expression.Constant(null)), Expression.Constant(DBNull.Value, typeof(object)),  Expression.Convert(propVal, typeof(object)))));
                body.Add(Expression.Call(props, add, par));
            }

            var lambda = Expression.Lambda<Action<NpgsqlCommand, T>>(Expression.Block(new[] { par }, body), cmd, parameters);
            return lambda.Compile();
        }

        public static IEnumerable<TResult> ExecuteReader<TResult, T>(this NpgsqlConnection conn, string sql, T parameters, Func<NpgsqlDataReader, TResult> mapper, NpgsqlTransaction transaction = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Transaction = transaction;

                ApplyParameters(cmd, parameters);

                cmd.Prepare();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return mapper(reader);
                    }
                }
            }
        }


        public static async Task<IEnumerable<TResult>> ExecuteReaderAsync<TResult, T>(this NpgsqlConnection conn, string sql, T parameters, Func<NpgsqlDataReader, TResult> mapper, NpgsqlTransaction transaction = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Transaction = transaction;

                ApplyParameters(cmd, parameters);

                cmd.Prepare();

                var it = (NpgsqlDataReader)await cmd.ExecuteReaderAsync();
                return MapIterator(it, mapper);
            }
        }

        private static IEnumerable<TResult> MapIterator<TResult>(NpgsqlDataReader reader, Func<NpgsqlDataReader, TResult> mapper)
        {
            using (reader)
            {
                while (reader.Read())
                {
                    yield return mapper(reader);
                }
            }
        }

        public static T ExecuteScalar<T>(this NpgsqlConnection conn, string sql, T parameters)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                ApplyParameters(cmd, parameters);

                object result = cmd.ExecuteScalar();
                if (result == DBNull.Value || result == null)
                {
                    return default(T);
                }

                return (T)result;
            }
        }
    }
}
