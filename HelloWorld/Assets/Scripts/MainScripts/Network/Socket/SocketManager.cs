public interface IDispatch
{
    public bool Deserialize(ushort id, UnsafeByteBuffer buffer);
    public void HandleSocketEvent(SocketEvent type, int code);
}
public class SocketManager : Singleton<SocketManager>
{
    private TransportBase socket;

    public ConnectState State => socket?.State ?? ConnectState.Idle;

    /// <summary>
    /// 创建自动连接
    /// </summary>
    public void Create<T>(string ip, ushort port, uint playerId, string token, IDispatch dispatch) where T : TransportBase, new()
    {
        Dispose();
        socket = new T();
        socket.Init(ip, port, playerId, token, dispatch);
    }
    public void Reconnect()
    {
        socket?.Reconnect();
    }
    public void Send(ushort id, ISerialize msg)
    {
        socket?.Send(id, msg);
    }
    public void Close()
    {
        socket?.Close();
    }
    public void Dispose()
    {
        socket?.Dispose();
    }
}