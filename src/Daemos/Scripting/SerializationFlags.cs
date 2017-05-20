﻿using System;

namespace Daemos.Scripting
{
    [Flags]
    public enum SerializationFlags : byte 
    {
        Null = 0x00,
        NotNull = 0x01,
        BinaryFormatter = 0x02 | NotNull,
        Serializable = 0x04 | NotNull,
        ProtoBuf = 0x08 | NotNull,
    }
}