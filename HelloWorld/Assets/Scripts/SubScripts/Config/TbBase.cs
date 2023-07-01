using Bright.Serialization;

namespace cfg
{
    public interface TbBase
    {
        public void Deserialize(ByteBuf _buf);
    }
}