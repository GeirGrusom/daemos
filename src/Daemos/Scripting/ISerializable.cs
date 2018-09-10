// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Scripting
{
    /// <summary>
    /// Defines an interface for serializable types
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializes this instance to the specified serializer target
        /// </summary>
        /// <param name="serializer">Serializer target to serialize to</param>
        void Serialize(IStateSerializer serializer);
    }
}
