using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Api.Models
{
    public static class TransactionMapper
    {
        public static TransactionResult ToTransactionResult(this Transaction input)
        {
            return new TransactionResult(input);
        }
    }
}
