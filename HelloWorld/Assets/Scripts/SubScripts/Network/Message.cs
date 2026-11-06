public interface ISerialize
{
    void Serialize(UnsafeByteBuffer buffer);
}
public interface IDeserialize
{
    void Deserialize(UnsafeByteBuffer buffer);
}