using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Postgres.Tests
{
    [TestFixture]
    public class QueryProviderTests
    {
        [Test]
        public void Foo()
        {
            var provider = new PostgreSqlQueryProvider(null, null);
            PostgreSqlOrderedQuerableProvider<Transaction> q = new PostgreSqlOrderedQuerableProvider<Transaction>(provider);
            var query = q.Where(x => x.Expires > DateTime.UtcNow);
            
            
        }

        [Test]
        public async Task Bar()
        {
            PostgreSqlTransactionStorage st = new PostgreSqlTransactionStorage();

            var factory = new TransactionFactory(st);
            var tr = await factory.StartTransaction();

            await tr.Free();
            Guid id = Guid.Parse("acb09d8e-a69c-4561-9ec1-4addb5719584");
            var results = st.Query().Where(x => x.Id == id).ToArray();
        }

    }
}
