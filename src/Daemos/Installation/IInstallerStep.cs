// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System.Collections.Generic;

    /// <summary>
    /// This interfaces defines a step in the installer
    /// </summary>
    public interface IInstallerStep
    {
        /// <summary>
        /// Gets the name of the step
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates an iterator for tasks for this installation step
        /// </summary>
        /// <returns>Iterator for installation task</returns>
        IEnumerable<ITask> GetStepTasks();
    }
}
