// <copyright file="CredentialsPromptTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Tests.Installation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Daemos.Installation;
    using Xunit;

    public class CredentialsPromptTests
    {
        private static ConsoleKeyInfo Enter = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        private static ConsoleKeyInfo Backspace = new ConsoleKeyInfo('\x07', ConsoleKey.Backspace, false, false, false);

        public static readonly object[][] testData =
        {
            new object[] {new[] {new ConsoleKeyInfo('F', ConsoleKey.F, false, false, false), Enter}, "F"},
            new object[] {new[] {new ConsoleKeyInfo('F', ConsoleKey.F, false, false, false), Backspace, new ConsoleKeyInfo('A', ConsoleKey.F, false, false, false), Enter}, "A"},
        };

        [Theory]
        [MemberData(nameof(testData))]
        public void TestCredentials(IEnumerable<ConsoleKeyInfo> keys, string expected)
        {
            var secret = ConsoleCredentialsPrompt.ReadSecret(keys);

            Assert.Equal(expected, secret);
        }

    }
}
