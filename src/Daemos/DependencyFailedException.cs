// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    /// <summary>
    /// This exception indicates that a type could not be resolved.
    /// </summary>
    public class DependencyFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyFailedException"/> class.
        /// </summary>
        /// <param name="type">Type that failed to resolve</param>
        public DependencyFailedException(Type type)
             : base("The type could not be resolved.")
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets the type that failed to resolve
        /// </summary>
        public Type Type { get; }
    }
}
