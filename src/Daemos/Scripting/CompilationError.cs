// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a compilation error
    /// </summary>
    public class CompilationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationError"/> class.
        /// </summary>
        /// <param name="line">Error line</param>
        /// <param name="column">Error column</param>
        /// <param name="message">Description message</param>
        public CompilationError(int line, int column, string message)
        {
            this.Line = line;
            this.Column = column;
            this.Message = message;
        }

        /// <summary>
        /// Line number where the error is present
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Column (or character on line) where the error is present
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Message describing the error
        /// </summary>
        public string Message { get; }

        
    }
}
