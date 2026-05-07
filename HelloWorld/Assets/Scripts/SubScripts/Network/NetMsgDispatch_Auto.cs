using System;

public class NetMsgId
{
    public const ushort Message_CSKcpConnect = 0;
    public const ushort Message_CSHeart = 1;
    public const ushort Message_CSMail = 100;
    public const ushort Message_CSReadMail = 101;
    public const ushort Message_CSGetMailReward = 102;
    public const ushort Message_CSGetMailAllReward = 103;
    public const ushort Message_CSDeleteMail = 104;
    public const ushort Message_CSDeleteAllMail = 105;
    public const ushort Message_SCKcpConnect = 10000;
    public const ushort Message_SCHeart = 10001;
    public const ushort Message_SCPlayerInfo = 10002;
    public const ushort Message_SCMail = 10100;
    public const ushort Message_SCGetMailReward = 10102;
    public const ushort Message_SCGetMailAllReward = 10103;
    public const ushort Message_SCDeleteMail = 10104;
}
public partial class NetMsgDispatch
{
    private bool Deserialize(ushort id, UnsafeByteBuffer buffer)
    {
        try
        {
            IDeserialize msg = null;
            switch (id)
            {
                case NetMsgId.Message_SCKcpConnect: msg = new Message.SCKcpConnect(); break;
                case NetMsgId.Message_SCHeart: msg = new Message.SCHeart(); break;
                case NetMsgId.Message_SCPlayerInfo: msg = new Message.SCPlayerInfo(); break;
                case NetMsgId.Message_SCMail: msg = new Message.SCMail(); break;
                case NetMsgId.Message_SCGetMailReward: msg = new Message.SCGetMailReward(); break;
                case NetMsgId.Message_SCGetMailAllReward: msg = new Message.SCGetMailAllReward(); break;
                case NetMsgId.Message_SCDeleteMail: msg = new Message.SCDeleteMail(); break;
            }
            msg.Deserialize(buffer);
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