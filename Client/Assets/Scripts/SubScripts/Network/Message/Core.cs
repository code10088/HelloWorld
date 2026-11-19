namespace Message
{
    public class CSSession : ISerialize
    {
        public string token;
        public int version;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteString(token);
            buffer.WriteInt(version);
        }
    }

    public class SCSession : IDeserialize
    {
        public int code;
        public uint conv;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            conv = buffer.ReadUInt();
        }
    }

    public class CSAuth : ISerialize
    {
        public string token;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteString(token);
        }
    }

    public class SCAuth : IDeserialize
    {
        public int code;
        public long time;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            time = buffer.ReadLong();
        }
    }

    public class CSHeart : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCHeart : IDeserialize
    {
        public long time;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            time = buffer.ReadLong();
        }
    }
}
