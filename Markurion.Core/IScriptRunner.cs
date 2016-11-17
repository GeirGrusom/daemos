using System.Threading.Tasks;

namespace Transact
{
    public interface IScriptRunner
    {
        Task Run(string code, Transaction transaction, Transaction previous);
    }
}