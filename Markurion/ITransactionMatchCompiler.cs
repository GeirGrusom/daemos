using System;
using System.Linq.Expressions;

namespace Markurion
{
    public interface ITransactionMatchCompiler
    {
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}