// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Linq.Expressions;

    public interface ITransactionMatchCompiler
    {
        Expression<Func<Transaction, bool>> BuildExpression(string input);
    }
}