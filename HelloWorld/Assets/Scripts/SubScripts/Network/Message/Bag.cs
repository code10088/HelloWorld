using System.Collections.Generic;

namespace Message
{
    public class BagData : ISerialize, IDeserialize
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

    public class EquipData : ISerialize, IDeserialize
    {
        public long uid;
        public uint equipId;
        public uint level;
        public uint bind;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteLong(uid);
            buffer.WriteUInt(equipId);
            buffer.WriteUInt(level);
            buffer.WriteUInt(bind);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            uid = buffer.ReadLong();
            equipId = buffer.ReadUInt();
            level = buffer.ReadUInt();
            bind = buffer.ReadUInt();
        }
    }

    public class CSBag : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCBag : IDeserialize
    {
        public List<BagData> items;
        public List<EquipData> equips;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            int itemCount = buffer.ReadInt();
            items = new List<BagData>();
            for (int i = 0; i < itemCount; i++)
            {
                var item = new BagData();
                item.Deserialize(buffer);
                items.Add(item);
            }
            int equipCount = buffer.ReadInt();
            equips = new List<EquipData>();
            for (int i = 0; i < equipCount; i++)
            {
                var equip = new EquipData();
                equip.Deserialize(buffer);
                equips.Add(equip);
            }
        }
    }

    public class CSUseItem : ISerialize
    {
        public uint itemId;
        public uint count;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(itemId);
            buffer.WriteUInt(count);
        }
    }

    public class SCUseItem : IDeserialize
    {
        public int code;
        public uint itemId;
        public uint count;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            itemId = buffer.ReadUInt();
            count = buffer.ReadUInt();
        }
    }
}
