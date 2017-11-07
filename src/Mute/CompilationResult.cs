// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Scripting;

    /// <summary>
    /// Represents the result of a compilation
    /// </summary>
    public sealed class CompilationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilationResult"/> class.
        /// </summary>
        /// <param name="result">Result is the program entry point</param>
        /// <param name="messages">Messages contain any compilation errors or warnings</param>
        public CompilationResult(Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> result, IEnumerable<CompilationMessage> messages)
        {
            this.Messages = messages.ToList();
            this.Success = this.Messages.Count(x => x.Severity == MessageSeverity.Error) == 0;
            this.Result = result;
        }

        /// <summary>
        /// Gets the compilation result entry point
        /// </summary>
        public Func<IStateSerializer, IStateDeserializer, IDependencyResolver, int> Result { get; }

        /// <summary>
        /// Gets the compilation messages produced by the compiler
        /// </summary>
        public List<CompilationMessage> Messages { get; }

        /// <summary>
        /// Gets a value indicating whether the compilation was successful or not
        /// </summary>
        public bool Success { get; }
    }
}
