using System;
using System.Collections.Generic;
using System.Linq;
using Daemos.Scripting;

namespace Daemos.Mute
{
    public sealed class CompilationResult
    {
        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> Result { get; }

        public List<CompilationMessage> Messages { get; }

        public bool Success { get; }

        public CompilationResult(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> result,
            IEnumerable<CompilationMessage> messages)
        {
            Messages = messages.ToList();
            Success = Messages.Count(x => x.Severity == MessageSeverity.Error) == 0;
            Result = result;
        }
    }

    public sealed class CompilationMessage
    {
        public string Message { get; }
        public int LineNumber { get; }
        public int Character { get; }
        public MessageSeverity Severity { get; }

        public CompilationMessage(string message, int lineNumber, int character, MessageSeverity severity)
        {
            Message = message;
            LineNumber = lineNumber;
            Character = character;
            Severity = severity;
        }
    }

    public enum MessageSeverity
    {
        Warning,
        Error
    }
}
