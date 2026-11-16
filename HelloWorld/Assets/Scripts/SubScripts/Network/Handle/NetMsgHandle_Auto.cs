using System;

public class NetMsgId
{
    public const ushort Message_CSSession = 0;
    public const ushort Message_CSAuth = 1;
    public const ushort Message_CSHeart = 2;
    public const ushort Message_CSMail = 100;
    public const ushort Message_CSReadMail = 101;
    public const ushort Message_CSGetMailReward = 102;
    public const ushort Message_CSGetMailAllReward = 103;
    public const ushort Message_CSDeleteMail = 104;
    public const ushort Message_CSDeleteAllMail = 105;
    public const ushort Message_CSBag = 106;
    public const ushort Message_CSUseItem = 107;
    public const ushort Message_SCSession = 10000;
    public const ushort Message_SCAuth = 10001;
    public const ushort Message_SCHeart = 10002;
    public const ushort Message_SCPlayerData = 10003;
    public const ushort Message_SCMail = 10100;
    public const ushort Message_SCGetMailReward = 10102;
    public const ushort Message_SCGetMailAllReward = 10103;
    public const ushort Message_SCDeleteMail = 10104;
    public const ushort Message_SCBag = 10106;
    public const ushort Message_SCUseItem = 10107;
}
public partial class NetMsgHandle
{
    public bool Deserialize(UnsafeByteBuffer buffer)
    {
        try
        {
            var id = buffer.ReadUShort();
            IDeserialize msg = null;
            switch (id)
            {
                case NetMsgId.Message_SCSession: msg = new Message.SCSession(); break;
                case NetMsgId.Message_SCAuth: msg = new Message.SCAuth(); break;
                case NetMsgId.Message_SCHeart: msg = new Message.SCHeart(); break;
                case NetMsgId.Message_SCPlayerData: msg = new Message.SCPlayerData(); break;
                case NetMsgId.Message_SCMail: msg = new Message.SCMail(); break;
                case NetMsgId.Message_SCGetMailReward: msg = new Message.SCGetMailReward(); break;
                case NetMsgId.Message_SCGetMailAllReward: msg = new Message.SCGetMailAllReward(); break;
                case NetMsgId.Message_SCDeleteMail: msg = new Message.SCDeleteMail(); break;
                case NetMsgId.Message_SCBag: msg = new Message.SCBag(); break;
                case NetMsgId.Message_SCUseItem: msg = new Message.SCUseItem(); break;
                default: return true;
            }
            msg.Deserialize(buffer);
            Handle(id, msg);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}