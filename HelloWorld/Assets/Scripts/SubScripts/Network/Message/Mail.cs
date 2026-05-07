using System.Collections.Generic;

namespace Message
{
    public enum MailType
    {
        System = 0,
    }

    public class MailDetail : ISerialize, IDeserialize
    {
        public uint mailId;
        public MailType type;
        public uint status;
        public long time;
        public string title;
        public string content;
        public List<RewardInfo> rewards = new List<RewardInfo>();

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(mailId);
            buffer.WriteInt((int)type);
            buffer.WriteUInt(status);
            buffer.WriteLong(time);
            buffer.WriteString(title);
            buffer.WriteString(content);
            buffer.WriteInt(rewards.Count);
            for (int i = 0; i < rewards.Count; i++)
            {
                rewards[i].Serialize(buffer);
            }
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            mailId = buffer.ReadUInt();
            type = (MailType)buffer.ReadInt();
            status = buffer.ReadUInt();
            time = buffer.ReadLong();
            title = buffer.ReadString();
            content = buffer.ReadString();
            int rewardCount = buffer.ReadInt();
            for (int i = 0; i < rewardCount; i++)
            {
                var reward = new RewardInfo();
                reward.Deserialize(buffer);
                rewards.Add(reward);
            }
        }
    }

    public class CSMail : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCMail : IDeserialize
    {
        public List<MailDetail> details = new List<MailDetail>();

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            int count = buffer.ReadInt();
            details.Clear();
            for (int i = 0; i < count; i++)
            {
                var detail = new MailDetail();
                detail.Deserialize(buffer);
                details.Add(detail);
            }
        }
    }

    public class CSReadMail : ISerialize
    {
        public uint[] lists;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUIntArray(lists);
        }
    }

    public class CSGetMailReward : ISerialize
    {
        public uint mailId;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(mailId);
        }
    }

    public class SCGetMailReward : IDeserialize
    {
        public int status;
        public uint mailId;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            status = buffer.ReadInt();
            mailId = buffer.ReadUInt();
        }
    }

    public class CSGetMailAllReward : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCGetMailAllReward : IDeserialize
    {
        public void Deserialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class CSDeleteMail : ISerialize
    {
        public uint mailId;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(mailId);
        }
    }

    public class SCDeleteMail : IDeserialize
    {
        public int status;
        public uint mailId;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            status = buffer.ReadInt();
            mailId = buffer.ReadUInt();
        }
    }

    public class CSDeleteAllMail : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }
}
