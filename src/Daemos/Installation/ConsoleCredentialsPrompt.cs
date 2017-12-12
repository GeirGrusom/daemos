// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Specifies a credential prompt for use with a console application.
    /// </summary>
    public class ConsoleCredentialsPrompt : ICredentialsPrompt
    {
        /// <summary>
        /// Reads a secret from the console input.
        /// </summary>
        /// <returns>The secret written in console input.</returns>
        public static string ReadSecret()
        {
            return ReadSecret(ReadKeys());
        }

        /// <summary>
        /// Reads a secret from the <see cref="ConsoleKeyInfo"/> iterator.
        /// </summary>
        /// <param name="keys">Keys to read.</param>
        /// <returns>The secret from the iterator.</returns>
        public static string ReadSecret(IEnumerable<ConsoleKeyInfo> keys)
        {
            var buffer = new StringBuilder();
            foreach (var key in keys)
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    return buffer.ToString();
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    return null;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length == 0)
                    {
                        continue;
                    }

                    buffer.Remove(buffer.Length - 1, 1);
                }

                if (!char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                }
            }

            throw new InvalidOperationException();
        }

        /// <inheritdoc/>
        public NetworkCredential ReadCredentials(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Username:");
            var username = Console.ReadLine();
            Console.WriteLine("Password:");
            var password = ReadSecret();
            if (password == null)
            {
                throw new InvalidCredentialsException("No password provided.");
            }

            return new NetworkCredential(username, password);
        }

        private static IEnumerable<ConsoleKeyInfo> ReadKeys()
        {
            while (true)
            {
                yield return Console.ReadKey(true);
            }
        }
    }
}
