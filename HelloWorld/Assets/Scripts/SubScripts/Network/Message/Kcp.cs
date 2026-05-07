namespace Message
{
    public class CSKcpConnect : ISerialize
    {
        public uint playerId;
        public string token;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(token);
        }
    }

    public class SCKcpConnect : IDeserialize
    {
        public uint conv;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            conv = buffer.ReadUInt();
        }
    }
}
