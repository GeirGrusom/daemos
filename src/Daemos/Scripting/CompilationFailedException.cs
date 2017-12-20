// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represent an exception where compilation failed
    /// </summary>
    public class CompilationFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationFailedException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="errors">List of compilation errors</param>
        public CompilationFailedException(string message, IEnumerable<CompilationError> errors)
            : base(message)
        {
            this.Errors = errors.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the compilation errors associated with this exception
        /// </summary>
        public IReadOnlyList<CompilationError> Errors { get; }
    }
}
