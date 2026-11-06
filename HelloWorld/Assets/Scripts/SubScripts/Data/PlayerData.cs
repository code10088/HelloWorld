using Message;

public class PlayerData : DataBase
{
    private uint playerId;
    private uint lv = 0;

    public uint PlayerId => playerId;
    public uint Lv => lv;

    public void Init()
    {
    }
    public void Clear()
    {
    }

    public void SCPlayerInfo(IDeserialize msg)
    {
        var info = (SCPlayerData)msg;
        playerId = info.data.playerId;
        lv = info.data.level;
        EventManager.Instance.Fire(EventType.RefreshPlayerData);
    }
}