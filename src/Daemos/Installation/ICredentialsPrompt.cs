// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System.Net;

    /// <summary>
    /// This interface declares a contract for types that can read credentials
    /// </summary>
    public interface ICredentialsPrompt
    {
        /// <summary>
        /// Reads credentials
        /// </summary>
        /// <param name="message">Message displayed to the user</param>
        /// <returns>The <see cref="NetworkCredential"/> produced.</returns>
        NetworkCredential ReadCredentials(string message);
    }
}