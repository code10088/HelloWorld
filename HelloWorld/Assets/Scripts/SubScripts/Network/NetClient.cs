public class NetClient : Singleton<NetClient>
{
    private IChannel channel;
    private ChannelHandle handle = new ChannelHandle();

    public ConnectState State => channel?.State ?? ConnectState.Idle;

    public void Create<T>(string ip, ushort port) where T : IChannel, new()
    {
        channel = new T();
        channel.Init(ip, port, handle);
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