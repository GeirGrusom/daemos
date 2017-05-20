using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Api.Models
{
    public static class TransactionMapper
    {
        public static TransactionResult Map(Transaction input)
        {
            return new TransactionResult
            {
                Id = input.Id,
                Revision = input.Revision,
                Created = input.Created,
                Expired = input.Expired,
                Expires = input.Expires,
                Payload = input.Payload,
                State = input.State,
                Handler = input.Handler,
            };
        }
    }
}
