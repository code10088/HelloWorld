public interface IDispatch
{
    public string Token { get; }
    public bool Deserialize(UnsafeByteBuffer buffer);
    public void HandleNetEvent(NetEvent type, int code);
}
public class NetClient : Singleton<NetClient>
{
    private ChannelInterface channel;

    public ConnectState State => channel?.State ?? ConnectState.Idle;

    public void Create<T>(string ip, ushort port, IDispatch dispatch) where T : ChannelInterface, new()
    {
        channel = new T();
        channel.Init(ip, port, dispatch);
    }
    public void Connect()
    {
        channel?.Connect();
    }
    public void Send(ushort id, ISerialize msg)
    {
        channel?.Send(id, msg);
    }
    public void Close()
    {
        channel?.Close();
    }
    public void Dispose()
    {
        channel?.Dispose();
    }
}