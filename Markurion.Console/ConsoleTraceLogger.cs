using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;

namespace Markurion.Console
{
    public class ConsoleTraceLogger : System.Diagnostics.Tracing.EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            System.Console.WriteLine($"{eventData.Level} ({eventData.EventSource}): {eventData.Message}");
        }
    }
}
