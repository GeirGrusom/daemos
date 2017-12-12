// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Implemetns a logger that offloads logging to a separate thread.
    /// </summary>
    /// <typeparam name="T">Subject to log for</typeparam>
    public class OffThreadLogger<T> : ILogger<T>
    {
        private readonly LoggingThread loggingThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="OffThreadLogger{T}"/> class.
        /// </summary>
        /// <param name="loggingThread">Thread to log to</param>
        public OffThreadLogger(LoggingThread loggingThread)
        {
            this.loggingThread = loggingThread;
        }

        /// <inheritdoc/>
        public Type SubjectType => typeof(T);

        /// <inheritdoc/>
        public void Log(LogLevel logLevel, string message, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string filename = null, [CallerLineNumber] int lineNumber = 0)
        {
            var obj = new LogObject(logLevel, Guid.NewGuid(), message, exception, callerMemberName, filename, lineNumber);

            this.loggingThread.Add(obj);
        }
    }
}
