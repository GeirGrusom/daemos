using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Daemos.Installation
{
    public interface ITask
    {
        Task Install();

        Task Rollback();
    }
}
