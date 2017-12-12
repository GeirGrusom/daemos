// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    public interface ISerializable
    {
        void Serialize(IStateSerializer serializer);
    }
}
