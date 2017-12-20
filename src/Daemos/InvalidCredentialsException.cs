// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an invalid credential exception
    /// </summary>
    [Serializable]
    public sealed class InvalidCredentialsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
        /// </summary>
        public InvalidCredentialsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidCredentialsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Exception that produced this exception</param>
        public InvalidCredentialsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
        /// </summary>
        /// <param name="info">Info used to retrieve serialization data</param>
        /// <param name="context">Context for streaming</param>
        public InvalidCredentialsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
