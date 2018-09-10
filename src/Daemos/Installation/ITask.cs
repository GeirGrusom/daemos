// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface defines an installer task
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Performs an installation for this task
        /// </summary>
        /// <returns>Asynchronous result</returns>
        Task Install();

        /// <summary>
        /// Rolls back this installation task if it has been installed
        /// </summary>
        /// <returns>Asynchronous result</returns>
        Task Rollback();
    }
}
