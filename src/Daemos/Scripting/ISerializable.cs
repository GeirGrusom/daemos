// <copyright file="ISerializable.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Scripting
{
    public interface ISerializable
    {
        void Serialize(IStateSerializer serializer);
    }
}
