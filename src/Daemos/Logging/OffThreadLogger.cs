using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Daemos.Logging
{
    /// <summary>
    /// Implemetns a logger that offloads logging to a separate thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OffThreadLogger<T> : ILogger<T>
    {
        private readonly LoggingThread loggingThread;

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
