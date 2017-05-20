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
