using System;
using System.Linq.Expressions;

namespace Daemos
{
    public interface ITransactionMatchCompiler
    {
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}