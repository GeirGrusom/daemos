// <copyright file="ITestSubject.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    using System.Threading.Tasks;

    public interface ITestSubject<T>
    {
        Task<T> GetSubjectAsync();
    }
}
