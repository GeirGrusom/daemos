using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Daemos.Threading
{
    public interface IAwaitableEvent
    {
        Task<bool> WaitOne();
        Task<bool> WaitOne(int timeout);
        Task<bool> WaitOne(CancellationToken cancel);
        Task<bool> WaitOne(int timeout, CancellationToken cancel);
    }
}
