﻿// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Npgsql;
    using NpgsqlTypes;

    /// <summary>
    /// This class is a LINQ query provider for PostgreSQL
    /// </summary>
    public class PostgreSqlQueryProvider : IQueryProvider
    {
        private static readonly Dictionary<Type, NpgsqlDbType> LookupDictionary = new Dictionary<Type, NpgsqlDbType>
        {
            [typeof(byte)] = NpgsqlDbType.Smallint,
            [typeof(short)] = NpgsqlDbType.Smallint,
            [typeof(int)] = NpgsqlDbType.Integer,
            [typeof(long)] = NpgsqlDbType.Bigint,
            [typeof(string)] = NpgsqlDbType.Varchar,
            [typeof(float)] = NpgsqlDbType.Real,
            [typeof(double)] = NpgsqlDbType.Double,
            [typeof(TransactionStatus)] = NpgsqlDbType.Integer,
            [typeof(bool)] = NpgsqlDbType.Boolean,
            [typeof(DateTime)] = NpgsqlDbType.Timestamp,
            [typeof(decimal)] = NpgsqlDbType.Money,
            [typeof(Guid)] = NpgsqlDbType.Uuid,
        };

        private readonly NpgsqlConnection connection;
        private readonly ITransactionStorage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlQueryProvider"/> class.
        /// </summary>
        /// <param name="conn">Database connection</param>
        /// <param name="storage">Storage engine</param>
        public PostgreSqlQueryProvider(NpgsqlConnection conn, ITransactionStorage storage)
        {
            this.connection = conn;
            this.storage = storage;
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            return (IQueryable)Activator.CreateInstance(typeof(PostgreSqlOrderedQuerableProvider<>).MakeGenericType(typeof(Transaction)), this, expression);
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new PostgreSqlOrderedQuerableProvider<TElement>(this, expression);
        }

        /// <inheritdoc/>
        public object Execute(Expression expression)
        {
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            return null;
        }

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression)
        {
            Type tr = typeof(TResult);
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            using (var cmd = this.connection.CreateCommand())
            {
                cmd.CommandText = exp;
                int id = 0;
                foreach (var param in visitor.Parameters)
                {
                    var p = new NpgsqlParameter("p" + (++id), param)
                    {
                        NpgsqlDbType = GetDbType(param.GetType())
                    };
                    cmd.Parameters.Add(p);
                }

                cmd.Prepare();

                using (var reader = cmd.ExecuteReader())
                {
                    if (tr.GenericTypeArguments.Length == 0)
                    {
                        // This is probably a scalar.
                        reader.Read();
                        object value = reader.GetValue(0);
                        if (value is DBNull)
                        {
                            return default(TResult);
                        }

                        return (TResult)(dynamic)value;
                    }

                    var rowResult = typeof(TResult).GenericTypeArguments[0];

                    if (rowResult == typeof(Transaction))
                    {
                        var results = new List<Transaction>();
                        while (reader.Read())
                        {
                            results.Add(this.MapTransaction(reader));
                        }

                        return (TResult)(object)results;
                    }
                }
            }

            return default(TResult);
        }

        private static Type GetElementType(Type expType)
        {
            var interf = expType.GetInterfaces().Single(x => x.Name == "IEnumerable`1");
            return interf.GetGenericArguments()[0];
        }

        private static NpgsqlDbType GetDbType(Type input)
        {
            return LookupDictionary[input];
        }

        private static T? GetValue<T>(NpgsqlDataReader reader, int ordinal)
            where T : struct
        {
            object v = reader.GetValue(ordinal);
            if (v == DBNull.Value)
            {
                return null;
            }

            return (T)v;
        }

        private static T GetObjectValue<T>(NpgsqlDataReader reader, int ordinal)
            where T : class
        {
            object v = reader.GetValue(ordinal);
            if (v == DBNull.Value)
            {
                return null;
            }

            return (T)v;
        }

        private Transaction MapTransaction(NpgsqlDataReader reader)
        {
            const int Id = 0;
            const int Revision = 1;
            const int Created = 2;
            const int Expires = 3;
            const int Expired = 4;
            const int Payload = 5;
            const int Script = 6;
            const int Status = 9;
            const int Error = 10;

            Guid? parentId = GetValue<Guid>(reader, 7);
            int? parentRevision = GetValue<int>(reader, 8);

            var parent = parentId != null ? new TransactionRevision(parentId.Value, parentRevision.Value) : (TransactionRevision?)null;

            var id = reader.GetGuid(Id);
            var rev = reader.GetInt32(Revision);
            var created = reader.GetDateTime(Created);
            var expires = GetValue<DateTime>(reader, Expires);
            var expired = GetValue<DateTime>(reader, Expired);
            string payloadValue = GetObjectValue<string>(reader, Payload);
            object payload = payloadValue != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(payloadValue) : null;

            var script = GetObjectValue<string>(reader, Script);
            var status = (TransactionStatus)reader.GetInt32(Status);

            string errorValue = GetObjectValue<string>(reader, Error);
            object error = errorValue != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(errorValue) : null;
            return new Transaction(id, rev, created, expires, expired, payload, script, status, parent, error, this.storage);
        }
    }
}
