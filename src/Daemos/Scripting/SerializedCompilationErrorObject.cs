// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System.Collections.Generic;
    using System.Linq;

    public class SerializedCompilationError
    {
        public SerializedCompilationError(string message, IEnumerable<CompilationError> errors)
        {
            this.Message = message;
            this.Errors = errors.ToList();
        }

        public string Message { get; }

        public List<CompilationError> Errors { get; }

        public static SerializedCompilationError FromException(CompilationFailedException ex)
        {
            return new SerializedCompilationError(ex.Message, ex.Errors);
        }
    }
}
