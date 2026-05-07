namespace Message
{
    public class BasePlayerInfo : ISerialize, IDeserialize
    {
        public uint playerId;
        public string nickName;
        public string avatar;
        public uint level;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(nickName);
            buffer.WriteString(avatar);
            buffer.WriteUInt(level);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            playerId = buffer.ReadUInt();
            nickName = buffer.ReadString();
            avatar = buffer.ReadString();
            level = buffer.ReadUInt();
        }
    }

    public class SCPlayerInfo : IDeserialize
    {
        public BasePlayerInfo info;
        public long registerTime;
        public uint mail;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            info = new BasePlayerInfo();
            info.Deserialize(buffer);
            registerTime = buffer.ReadLong();
            mail = buffer.ReadUInt();
        }
    }
}
