using System.Collections.Generic;

namespace Message
{
    public class FriendInfo : ISerialize, IDeserialize
    {
        public uint playerId;
        public string nickName;
        public string avatar;
        public uint level;
        public string remark;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(nickName);
            buffer.WriteString(avatar);
            buffer.WriteUInt(level);
            buffer.WriteString(remark);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            playerId = buffer.ReadUInt();
            nickName = buffer.ReadString();
            avatar = buffer.ReadString();
            level = buffer.ReadUInt();
            remark = buffer.ReadString();
        }
    }

    public class FriendApplyInfo : ISerialize, IDeserialize
    {
        public uint playerId;
        public string nickName;
        public string avatar;
        public uint level;
        public long time;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(nickName);
            buffer.WriteString(avatar);
            buffer.WriteUInt(level);
            buffer.WriteLong(time);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            playerId = buffer.ReadUInt();
            nickName = buffer.ReadString();
            avatar = buffer.ReadString();
            level = buffer.ReadUInt();
            time = buffer.ReadLong();
        }
    }

    public class SearchPlayerInfo : ISerialize, IDeserialize
    {
        public uint playerId;
        public string nickName;
        public string avatar;
        public uint level;
        public uint relation;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(nickName);
            buffer.WriteString(avatar);
            buffer.WriteUInt(level);
            buffer.WriteUInt(relation);
        }

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            playerId = buffer.ReadUInt();
            nickName = buffer.ReadString();
            avatar = buffer.ReadString();
            level = buffer.ReadUInt();
            relation = buffer.ReadUInt();
        }
    }

    public class CSFriendList : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCFriendList : IDeserialize
    {
        public List<FriendInfo> friends;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            int count = buffer.ReadInt();
            friends = new List<FriendInfo>();
            for (int i = 0; i < count; i++)
            {
                var friend = new FriendInfo();
                friend.Deserialize(buffer);
                friends.Add(friend);
            }
        }
    }

    public class CSFindFriend : ISerialize
    {
        public string nickName;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteString(nickName);
        }
    }

    public class SCFindFriend : IDeserialize
    {
        public List<SearchPlayerInfo> players;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            int count = buffer.ReadInt();
            players = new List<SearchPlayerInfo>();
            for (int i = 0; i < count; i++)
            {
                var player = new SearchPlayerInfo();
                player.Deserialize(buffer);
                players.Add(player);
            }
        }
    }

    public class CSAddFriend : ISerialize
    {
        public uint playerId;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
        }
    }

    public class SCAddFriend : IDeserialize
    {
        public int code;
        public uint playerId;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            playerId = buffer.ReadUInt();
        }
    }

    public class CSFriendApplyList : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCFriendApplyList : IDeserialize
    {
        public List<FriendApplyInfo> applies;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            int count = buffer.ReadInt();
            applies = new List<FriendApplyInfo>();
            for (int i = 0; i < count; i++)
            {
                var applie = new FriendApplyInfo();
                applie.Deserialize(buffer);
                applies.Add(applie);
            }
        }
    }

    public class CSHandleFriendApply : ISerialize
    {
        public uint playerId;
        public uint handle;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteUInt(handle);
        }
    }

    public class SCHandleFriendApply : IDeserialize
    {
        public int code;
        public uint playerId;
        public uint handle;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            playerId = buffer.ReadUInt();
            handle = buffer.ReadUInt();
        }
    }

    public class CSAcceptAllFriendApply : ISerialize
    {
        public void Serialize(UnsafeByteBuffer buffer)
        {
        }
    }

    public class SCAcceptAllFriendApply : IDeserialize
    {
        public int code;
        public uint count;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            count = buffer.ReadUInt();
        }
    }

    public class CSDeleteFriend : ISerialize
    {
        public uint playerId;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
        }
    }

    public class SCDeleteFriend : IDeserialize
    {
        public int code;
        public uint playerId;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            playerId = buffer.ReadUInt();
        }
    }

    public class CSSetFriendRemark : ISerialize
    {
        public uint playerId;
        public string remark;

        public void Serialize(UnsafeByteBuffer buffer)
        {
            buffer.WriteUInt(playerId);
            buffer.WriteString(remark);
        }
    }

    public class SCSetFriendRemark : IDeserialize
    {
        public int code;
        public uint playerId;
        public string remark;

        public void Deserialize(UnsafeByteBuffer buffer)
        {
            code = buffer.ReadInt();
            playerId = buffer.ReadUInt();
            remark = buffer.ReadString();
        }
    }
}
