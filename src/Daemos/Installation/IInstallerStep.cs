// <copyright file="IInstallerStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Daemos.Installation
{
    public interface IInstallerStep
    {
        string Name { get; }

        IEnumerable<ITask> GetStepTasks();
    }
}
