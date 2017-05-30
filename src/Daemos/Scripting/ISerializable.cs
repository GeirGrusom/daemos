namespace Daemos.Scripting
{
    public interface ISerializable
    {
        void Serialize(IStateSerializer serializer);
    }
}
