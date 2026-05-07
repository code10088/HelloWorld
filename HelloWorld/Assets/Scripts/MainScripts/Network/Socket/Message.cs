public class NetMsgId
{
    public const ushort CSKcpConnect = 0;
    public const ushort CSHeart = 1;
    public const ushort SCKcpConnect = 10000;
    public const ushort SCHeart = 10001;
}
public interface ISerialize
{
    void Serialize(UnsafeByteBuffer buffer);
}
public interface IDeserialize
{
    void Deserialize(UnsafeByteBuffer buffer);
}
public class CS_KcpConnect : ISerialize
{
    public uint playerId;
    public string token;

    public void Serialize(UnsafeByteBuffer buffer)
    {
        buffer.WriteUInt(playerId);
        buffer.WriteString(token);
    }
}
public class CS_Heart : ISerialize
{
    public void Serialize(UnsafeByteBuffer buffer)
    {
    }
}