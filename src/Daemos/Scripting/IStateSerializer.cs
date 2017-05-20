﻿using System;

namespace Daemos.Scripting
{
    public interface IStateSerializer : IDisposable
    {
        byte[] GetState();
        void Serialize<T>(string name, T value);
        void SerializeNull(string name, Type type);
        void WriteEndStage();
        void WriteStage(int stage);

        System.IO.Stream UnderlyingStream { get; }
    }
}