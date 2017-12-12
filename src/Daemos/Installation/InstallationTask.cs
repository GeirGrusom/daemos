// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public interface ITask
    {
        Task Install();

        Task Rollback();
    }
}
