// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Determines log level
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace level. Used for debugging
        /// </summary>
        Trace,

        /// <summary>
        /// Debug. Used for debugging
        /// </summary>
        Debug,

        /// <summary>
        /// Information. Used to log non-errors but messages useful for diagnostics
        /// </summary>
        Info,

        /// <summary>
        /// Warning. Used to log potential issues.
        /// </summary>
        Warning,

        /// <summary>
        /// Error. Used to log errors.
        /// </summary>
        Error,

        /// <summary>
        /// Critical. Used to log severe errors that end in application termination or data corruption.
        /// </summary>
        Critical
    }
}
