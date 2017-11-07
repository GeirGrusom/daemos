// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute
{
    /// <summary>
    /// Represents a message produced during compilation.
    /// </summary>
    public sealed class CompilationMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationMessage"/> class.
        /// </summary>
        /// <param name="message">The compilation message text</param>
        /// <param name="lineNumber">Line number in source code relevant to the message</param>
        /// <param name="character">Column in the source code relevant to the message</param>
        /// <param name="severity">Message severity</param>
        public CompilationMessage(string message, int lineNumber, int character, MessageSeverity severity)
        {
            this.Message = message;
            this.LineNumber = lineNumber;
            this.Character = character;
            this.Severity = severity;
        }

        /// <summary>
        /// Gets the compilation message text
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the compilation message liner number
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the compilation message column
        /// </summary>
        public int Character { get; }

        /// <summary>
        /// Gets the compilation message severity
        /// </summary>
        public MessageSeverity Severity { get; }
    }
}
