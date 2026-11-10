using Message;

public class PlayerData : DataBase
{
    private uint playerId;
    private uint lv = 0;

    public uint PlayerId => playerId;
    public uint Lv => lv;

    public void Init()
    {
        NetClient.Instance.Register(NetMsgId.Message_SCPlayerData, SCPlayerInfo);
    }
    public void Clear()
    {
        NetClient.Instance.Unregister(NetMsgId.Message_SCPlayerData);
    }

    private void SCPlayerInfo(IDeserialize msg)
    {
        var info = (SCPlayerData)msg;
        playerId = info.data.playerId;
        lv = info.data.level;
        EventManager.Instance.Fire(EventType.RefreshPlayerData);
    }
}