using System.Net;

namespace Daemos.Installation
{
    public interface ICredentialsPrompt
    {
        NetworkCredential ReadCredentials(string message);
    }
}