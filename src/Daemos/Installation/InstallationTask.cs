using System;
using System.Collections.Generic;
using System.Text;

namespace Daemos.Installation
{
    public interface ITask
    {
        void Install();

        void Rollback();
    }
}
