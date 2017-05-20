namespace Daemos.Scripting
{
    public interface ISerializable
    {
        void Serialize(IStateSerializer serializer);

        System.IO.Stream UnderlyingStream { get; }
    }
}
