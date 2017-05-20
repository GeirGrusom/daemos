using System;
using System.Threading.Tasks;

namespace Transact
{
    public interface IScriptRunner
    {
        Task<TransactionMutableData> Run(string code, Transaction transaction);

        Task<Func<Transaction, Task<TransactionMutableData>>> Compile(string code);
    }
}