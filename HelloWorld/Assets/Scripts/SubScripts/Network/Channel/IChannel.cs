public enum ConnectState
{
    Idle,
    Connect,
    Connected,
    Close,
}
public enum NetEvent
{
    Reconnect,
    Connected,
    ConnectError,
    InvalidToken,
}
public interface IChannel
{
    public ConnectState State { get; }
    public void Init(string ip, ushort port, IChannelHandle handle);
    public void Connect();
    public void Close();
    public void Dispose();
    public void Send(ushort id, ISerialize msg);
}
public interface IChannelHandle
{
    public string Token { get; }
    public void HandleEvent(NetEvent type, int param);
    public bool Deserialize(UnsafeByteBuffer buffer);
}
public struct SendItem
{
    private ushort id;
    private ISerialize msg;
    public SendItem(ushort id, ISerialize msg)
    {
        this.id = id;
        this.msg = msg;
    }
    public void Serialize(UnsafeByteBuffer buffer, bool writelength = false)
    {
        buffer.SetWPos(0);
        if (writelength) buffer.WriteInt(0);
        buffer.WriteUShort(id);
        msg.Serialize(buffer);
        if (writelength) buffer.WriteValueAt(0, buffer.WPos - 4);
    }
}
