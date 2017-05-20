using Npgsql;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Transact.Postgres
{
    public class PostgreSqlQueryProvider : IQueryProvider
    {

        private readonly NpgsqlConnection connection;
        private readonly ITransactionStorage storage;

        public PostgreSqlQueryProvider(NpgsqlConnection conn, ITransactionStorage storage)
        {
            connection = conn;
            this.storage = storage;
        }

        private static Type GetElementType(Type expType)
        {
            // Unsupported in .NET Core 1.0 for some odd reason. Slated for 1.2 as far as I can tell.
            var interf = expType.GetInterfaces().Single(x => x.Name == "IEnumerable`1");
            return interf.GetGenericArguments()[0];            
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return (IQueryable)Activator.CreateInstance(typeof(PostgreSqlOrderedQuerableProvider<>).MakeGenericType(typeof(Transaction)), this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new PostgreSqlOrderedQuerableProvider<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            return null;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            PostgresVisitor visitor = new PostgresVisitor();
            visitor.Visit(expression);
            var exp = visitor.ToString();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = exp;
                int id = 0;
                foreach(var param in visitor.Parameters)
                {
                    cmd.Parameters.Add(new NpgsqlParameter("p" + (++id), param));
                }

                using (var reader = cmd.ExecuteReader())
                {

                    var rowResult = typeof(TResult).GenericTypeArguments[0];

                    if (rowResult == typeof(Transaction))
                    {
                        var results = new System.Collections.Generic.List<Transaction>();
                        while (reader.Read())
                        {
                            results.Add(MapTransaction(reader));
                        }

                        return (TResult)(object)results;
                    }
                }
            }
            return default(TResult);
        }

        private static T? GetValue<T>(NpgsqlDataReader reader, int ordinal)
            where T : struct
        {
            object v = reader.GetValue(ordinal);
            if (v == DBNull.Value)
                return null;
            return (T)v;
        }

        private static T GetObjectValue<T>(NpgsqlDataReader reader, int ordinal)
            where T : class
        {
            object v = reader.GetValue(ordinal);
            if (v == DBNull.Value)
                return null;
            return (T)v;
        }

        private Transaction MapTransaction(NpgsqlDataReader reader)
        {
                        

            Guid? parentId = GetValue<Guid>(reader, 7);
            int? parentRevision = GetValue<int>(reader, 8);

            var parent = parentId != null ? new TransactionRevision(parentId.Value, parentRevision.Value) : (TransactionRevision?)null;
            //    0        1             2               3         4             5             6              7           8              9
            // "\"Id\", \"Revision\", \"Created\", \"Expires\", \"Expired\", \"Payload\", \"Script\", \"ParentId\", \"ParentRevision\", \"State\""
            var id = reader.GetGuid(0);
            var rev = reader.GetInt32(1);
            var created = reader.GetDateTime(2);
            var expires = GetValue<DateTime>(reader, 3);
            var expired = GetValue<DateTime>(reader, 4);
            var payload = Newtonsoft.Json.JsonConvert.DeserializeObject(GetObjectValue<string>(reader, 5));
            var script = GetObjectValue<string>(reader, 6);
            var state = (TransactionState)reader.GetInt32(9);
            return new Transaction(id, rev, created, expires, expired, payload, script, state, parent , storage);
        } 
    }
}
