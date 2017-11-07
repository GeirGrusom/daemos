// <copyright file="IStateDeserializer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Daemos.Scripting
{
    public interface IStateDeserializer : IDisposable
    {
        T Deserialize<T>(string name);

        object Deserialize(string name, Type expectedType);

        int ReadStage();
    }
}