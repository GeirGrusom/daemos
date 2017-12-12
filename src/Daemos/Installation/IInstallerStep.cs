// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IInstallerStep
    {
        string Name { get; }

        IEnumerable<ITask> GetStepTasks();
    }
}
