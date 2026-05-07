namespace Message
{
    public class RewardInfo : ISerialize, IDeserialize
    {
        public uint itemId;
        public uint count;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(itemId);
            buffer.WriteUInt(count);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            itemId = buffer.ReadUInt();
            count = buffer.ReadUInt();
        }
    }
}
