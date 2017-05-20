using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Transact.Query
{
    public class Compiler : ITransactionMatchCompiler
    {
        public Expression<Func<Transaction, bool>> BuildExpression(string input)
        {
            throw new NotImplementedException();
        }
    }
}
