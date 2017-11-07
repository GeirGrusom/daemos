// <copyright file="ICredentialsPrompt.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;

namespace Daemos.Installation
{
    public interface ICredentialsPrompt
    {
        NetworkCredential ReadCredentials(string message);
    }
}