using Message;

public class PlayerData : DataBase
{
    private uint playerId;
    private uint lv = 0;

    public uint PlayerId => playerId;
    public uint Lv => lv;

    public void Init()
    {
        NetMsgDispatch.Instance.Register(NetMsgId.Message_SCPlayerInfo, SCPlayerInfo);
    }
    public void Clear()
    {
        NetMsgDispatch.Instance.UnRegister(NetMsgId.Message_SCPlayerInfo);
    }

    private void SCPlayerInfo(IDeserialize msg)
    {
        var info = (SCPlayerInfo)msg;
        playerId = info.info.playerId;
        lv = info.info.level;
    }
}