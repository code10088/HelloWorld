using System.Collections.Generic;

namespace Message
{
    public class BasePlayerData : ISerialize, IDeserialize
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

    public class SCPlayerData : IDeserialize
    {
        public BasePlayerData data;
        public long registerTime;
        public uint mail;
        public List<BagData> items;
        public List<EquipData> equips;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            data = new BasePlayerData();
            data.Deserialize(buffer);
            registerTime = buffer.ReadLong();
            mail = buffer.ReadUInt();
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
}
