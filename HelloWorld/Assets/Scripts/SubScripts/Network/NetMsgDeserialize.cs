using ProtoBuf;
using System;
public class NetMsgId
{
    public const ushort Message_CSHeart = 0;
    public const ushort Message_CSMail = 100;
    public const ushort Message_SCHeart = 10000;
    public const ushort Message_SCPlayerInfo = 10001;
    public const ushort Message_SCMail = 10100;
}
public partial class NetMsgDispatch
{
    private bool Deserialize(ushort id, Memory<byte> memory)
    {
        try
        {
            IExtensible msg = null;
            switch (id)
            {
                case NetMsgId.Message_SCHeart: msg = Serializer.Deserialize<Message.SCHeart>(memory); break;
                case NetMsgId.Message_SCPlayerInfo: msg = Serializer.Deserialize<Message.SCPlayerInfo>(memory); break;
                case NetMsgId.Message_SCMail: msg = Serializer.Deserialize<Message.SCMail>(memory); break;
            }
            HandleMsg(id, msg);
            return true;
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            return false;
        }
    }
}