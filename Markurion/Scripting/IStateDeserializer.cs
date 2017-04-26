using System;

namespace Markurion.Scripting
{
    public interface IStateDeserializer : IDisposable
    {
        T Deserialize<T>(string name);
        object Deserialize(string name, Type expectedType);
        int ReadStage();
    }
}