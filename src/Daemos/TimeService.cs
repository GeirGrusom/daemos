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

    public sealed class ConstantTimeService : ITimeService
    {
        private readonly DateTime _now;

        public ConstantTimeService(DateTime value)
        {
            _now = value;
        }

        public DateTime Now() => _now;
    }
}
