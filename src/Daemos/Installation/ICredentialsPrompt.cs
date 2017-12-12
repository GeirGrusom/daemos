// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System.Net;

    public interface ICredentialsPrompt
    {
        NetworkCredential ReadCredentials(string message);
    }
}