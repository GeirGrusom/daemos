// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Logging
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Defines a Logger interface
    /// </summary>
    /// <typeparam name="T">Subject type</typeparam>
    public interface ILogger<T>
    {
        /// <summary>
        /// Gets the logger subject type
        /// </summary>
        Type SubjectType { get; }

        /// <summary>
        /// Logs a new event
        /// </summary>
        /// <param name="logLevel">Log level for the new event</param>
        /// <param name="message">Event message</param>
        /// <param name="exception">Event exception</param>
        /// <param name="callerMemberName">Caller member name</param>
        /// <param name="filename">Caller filename</param>
        /// <param name="lineNumber">Caller file line number</param>
        void Log(LogLevel logLevel, string message, Exception exception, [CallerMemberName]string callerMemberName = null, [CallerFilePath] string filename = null, [CallerLineNumber]int lineNumber = 0);
    }
}
