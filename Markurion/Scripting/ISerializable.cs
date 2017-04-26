using System;
using System.Collections.Generic;
using System.Text;

namespace Markurion.Scripting
{
    public interface ISerializable
    {
        void Serialize(IStateSerializer serializer);

        System.IO.Stream UnderlyingStream { get; }
    }
}
