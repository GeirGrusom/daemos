using System;
using System.Linq.Expressions;

namespace Transact
{
    public interface ITransactionMatchCompiler
    {
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}