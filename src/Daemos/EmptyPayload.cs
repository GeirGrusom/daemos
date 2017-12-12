// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    /// <summary>
    /// Specifies an empty payload.
    /// </summary>
    public sealed class EmptyPayload
    {
        private EmptyPayload()
        {
        }

        /// <summary>
        /// Gets the single instance of this type.
        /// </summary>
        public static EmptyPayload Instance { get; } = new EmptyPayload();
    }
}
