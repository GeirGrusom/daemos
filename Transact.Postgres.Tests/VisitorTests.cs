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
    public class VisitorTests
    {
        [Test]
        public void Timestamp_Now()
        {
            var visitor = new PostgresVisitor();
            Expression<Func<IQueryable<Transaction>, IQueryable<Transaction>>> exp;
            exp = x => x.Where(t => t.Created < DateTime.Now);

            visitor.Visit(exp);

            Assert.That(visitor.ToString(), Is.EqualTo("SELECT * FROM tr.\"Transactions\" WHERE \"Created\" < timestamp (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')"));
        }
    }
}
