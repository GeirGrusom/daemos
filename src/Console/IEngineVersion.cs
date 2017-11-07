// <copyright file="IEngineVersion.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Console
{
    public interface IEchoService
    {
        void Print(string message);
    }

    public class EchoService : IEchoService
    {
        public void Print(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}
