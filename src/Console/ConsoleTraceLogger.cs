using System.Diagnostics.Tracing;

namespace Daemos.Console
{
    public class ConsoleTraceLogger : System.Diagnostics.Tracing.EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            System.Console.WriteLine($"{eventData.Level} ({eventData.EventSource}): {eventData.Message}");
        }
    }
}
