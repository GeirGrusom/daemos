namespace Daemos.Logging
{
    using System;

    /// <summary>
    /// Represents an object to be logged
    /// </summary>
    public class LogObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogObject"/> class.
        /// </summary>
        /// <param name="logLevel">Log level</param>
        /// <param name="traceId">Trace Id</param>
        /// <param name="message">Message of the log event</param>
        /// <param name="exception">Exception provided</param>
        /// <param name="memberName">Member name</param>
        /// <param name="filename">Log event filename</param>
        /// <param name="lineNumber">Log event line number</param>
        public LogObject(LogLevel logLevel, Guid traceId, string message, Exception exception, string memberName, string filename, int lineNumber)
        {
            this.LogLevel = logLevel;
            this.Message = message;
            this.Exception = exception;
            this.MemberName = memberName;
            this.Filename = filename;
            this.LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the log level for this event
        /// </summary>
        public LogLevel LogLevel { get; }

        /// <summary>
        /// Gets the trace id used to indicate log event relations
        /// </summary>
        public Guid TraceId { get; }

        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception provided
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the filename where the log statement was executed
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets the caller member name
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Gets the line number in the filename where the log statement was executed
        /// </summary>
        public int LineNumber { get; }
    }
}
