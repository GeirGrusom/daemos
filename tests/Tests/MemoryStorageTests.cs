// <copyright file="MemoryStorageTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests
{
    public class MemoryStorageTests : TransactionStorageTests<MemoryStorage>
    {
        protected override MemoryStorage CreateStorage()
        {
            return new MemoryStorage();
        }

        protected override MemoryStorage CreateStorage(ITimeService timeService)
        {
            return new MemoryStorage(timeService);
        }
    }
}
