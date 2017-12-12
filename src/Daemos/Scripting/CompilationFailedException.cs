// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CompilationError
    {
        public int Line { get; }

        public int Column { get; }

        public string Message { get; }

        public CompilationError(int line, int column, string message)
        {
            this.Line = line;
            this.Column = column;
            this.Message = message;
        }
    }

    public class CompilationFailedException : Exception
    {
        public IReadOnlyList<CompilationError> Errors { get; }

        public CompilationFailedException(string message, IEnumerable<CompilationError> errors)
            : base(message)
        {
            this.Errors = errors.ToList().AsReadOnly();
        }
    }
}
