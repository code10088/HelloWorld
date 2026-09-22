public interface IDispatch
{
    public bool Deserialize(ushort id, UnsafeByteBuffer buffer);
    public void HandleNetEvent(NetEvent type, int code);
}
public class NetClient : Singleton<NetClient>
{
    private TransportBase transport;

    public ConnectState State => transport?.State ?? ConnectState.Idle;

    /// <summary>
    /// 创建自动连接
    /// </summary>
    public void Create<T>(string ip, ushort port, uint playerId, string token, IDispatch dispatch) where T : TransportBase, new()
    {
        Dispose();
        transport = new T();
        transport.Init(ip, port, playerId, token, dispatch);
    }
    public void Reconnect()
    {
        transport?.Reconnect();
    }
    public void Send(ushort id, ISerialize msg)
    {
        transport?.Send(id, msg);
    }
    public void Close()
    {
        transport?.Close();
    }
    public void Dispose()
    {
        transport?.Dispose();
    }
}