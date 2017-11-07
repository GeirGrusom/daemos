// <copyright file="ConsoleTraceLogger.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Console
{
    using System.Diagnostics.Tracing;

    public class ConsoleTraceLogger : System.Diagnostics.Tracing.EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            System.Console.WriteLine($"{eventData.Level} ({eventData.EventSource}): {eventData.Message}");
        }
    }
}
