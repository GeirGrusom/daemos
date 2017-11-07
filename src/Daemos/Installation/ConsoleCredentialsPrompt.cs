// <copyright file="ConsoleCredentialsPrompt.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Daemos.Installation
{
    public class ConsoleCredentialsPrompt : ICredentialsPrompt
    {
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

        public static string ReadSecret()
        {
            return ReadSecret(ReadKeys());
        }

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
                if (!Char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                }
            }
            throw new InvalidOperationException();
        }
    }
}
