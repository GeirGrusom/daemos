// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

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
        private readonly DateTime now;

        public ConstantTimeService(DateTime value)
        {
            this.now = value;
        }

        public DateTime Now() => this.now;
    }
}
