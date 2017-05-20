using System;

namespace Daemos
{
    public interface ITimeService
    {
        DateTime Now();
    }

    public sealed class UtcTimeService : ITimeService
    {
        public DateTime Now() => DateTime.UtcNow;
    }
}
