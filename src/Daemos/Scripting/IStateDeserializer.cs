// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    using System;

    public interface IStateDeserializer : IDisposable
    {
        T Deserialize<T>(string name);

        object Deserialize(string name, Type expectedType);

        int ReadStage();
    }
}