// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAwaitableEvent
    {
        Task<bool> WaitOne();

        Task<bool> WaitOne(int timeout);

        Task<bool> WaitOne(CancellationToken cancel);

        Task<bool> WaitOne(int timeout, CancellationToken cancel);
    }
}
