using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion
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
