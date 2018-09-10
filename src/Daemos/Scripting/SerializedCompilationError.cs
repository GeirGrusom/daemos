// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class describes a serialization error
    /// </summary>
    public class SerializedCompilationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedCompilationError"/> class.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errors">Compilation errors</param>
        public SerializedCompilationError(string message, IEnumerable<CompilationError> errors)
        {
            this.Message = message;
            this.Errors = errors.ToList();
        }

        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets compilation errors produced by the serialization
        /// </summary>
        public List<CompilationError> Errors { get; }

        /// <summary>
        /// Creates a <see cref="SerializedCompilationError"/> from a <see cref="CompilationFailedException"/>
        /// </summary>
        /// <param name="ex">Exception to produce <see cref="SerializedCompilationError"/> from</param>
        /// <returns>Resulting object</returns>
        public static SerializedCompilationError FromException(CompilationFailedException ex)
        {
            return new SerializedCompilationError(ex.Message, ex.Errors);
        }
    }
}
