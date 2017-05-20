using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Transact;
using Transact.Api;

namespace UnitTests
{
    [TestFixture]
    public class TransactionMatchCompilerTests
    {
        [TestCase("1 = 1")]
        [TestCase("1 + 1 = 2")]
        [TestCase("2 = 1 + 1")]
        [TestCase("2 + 3 * 4 = 14")]
        [TestCase("2 * 3 + 4 = 10")]
        [TestCase("'2016-01-01Z' > '2015-01-01Z'")]
        [TestCase("true = true")]
        [TestCase("false = false")]
        [TestCase("null = null")]
        public void BuildExpression_ReturnsTrue(string expression)
        {
            var trans = new TransactionMatchCompiler();

            var func = trans.BuildExpression(expression).Compile();

            Assert.That(func(null), Is.True);
        }

        [TestCase("1 = 2")]
        [TestCase("1 + 1 = 3")]
        [TestCase("4 = 1 + 1")]
        [TestCase("1 + 3 * 4 = 14")]
        [TestCase("9 * 3 + 4 = 10")]
        [TestCase("'2016-01-01Z' < '2015-01-01Z'")]
        [TestCase("true = false")]
        [TestCase("false = true")]
        [TestCase("null != null")]
        public void BuildExpression_ReturnsFalse(string expression)
        {
            var trans = new TransactionMatchCompiler();

            var func = trans.BuildExpression(expression).Compile();

            Assert.That(func(null), Is.False);
        }

        private const string Id = "B3AFD6DBD01F447C847B23C56742CC03";
        private const string Created = "2015-01-01Z";
        [TestCase("id = {" + Id + "}")]
        [TestCase("created < '2016-01-01Z'")]
        [TestCase("state = \"initialized\"")]
        [TestCase("state != \"failed\"")]
        public void BuildExpression_MatchTransaction(string expression)
        {
            var trans = new TransactionMatchCompiler();
            var transaction = new Transaction(Guid.ParseExact(Id, "N"), 0, DateTime.ParseExact(Created, "yyyy'-'MM'-'dd'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), null, null, null, null, TransactionState.Initialized, null, null);

            var func = trans.BuildExpression(expression).Compile();

            Assert.That(func(transaction), Is.True);
        }

        [Test]
        public void BuildExpression_StringInArray()
        {
            var trans = new TransactionMatchCompiler();

            var func = trans.BuildExpression("\"foo\" in [\"foo\",\"bar\"]").Compile();

            Assert.That(func(null), Is.True);
        }

        [Test]
        public void BuildExpression_StateInArray()
        {
            var trans = new TransactionMatchCompiler();
            var transaction = new Transaction(Guid.ParseExact(Id, "N"), 0, DateTime.ParseExact(Created, "yyyy'-'MM'-'dd'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), null, null, null, null, TransactionState.Initialized, null, null);
            var func = trans.BuildExpression("state in [\"initialized\"]").Compile();

            Assert.That(func(transaction), Is.True);
        }

        [Test]
        public void BuildExpression_MatchTransaction_OnState()
        {
            var trans = new TransactionMatchCompiler();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null);

            var func = trans.BuildExpression("state = \"initialized\"").Compile();

            Assert.That(func(transaction), Is.True);
        }

        [Test]
        public void BuildExpression_MatchTransaction_OnNotEqualState()
        {
            var trans = new TransactionMatchCompiler();
            var transaction = new Transaction(Guid.NewGuid(), 0, DateTime.UtcNow, null, null, null, null, TransactionState.Initialized, null, null);

            var func = trans.BuildExpression("state != \"failed\"").Compile();

            Assert.That(func(transaction), Is.EqualTo(true));
        }
    }
}
