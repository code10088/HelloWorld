using System;

public class SocketManager : Singleton<SocketManager>
{
    private SBase socket;
    private Func<ushort, UnsafeByteBuffer, bool> deserialize;
    private Action<int, int> socketevent;

    public bool Connected => socket != null && socket.Connected;

    public void SetFunc(Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
    }
    /// <summary>
    /// 创建自动连接
    /// </summary>
    public void Create<T>(string ip, ushort port, uint playerId, string token) where T : SBase, new()
    {
        Dispose();
        socket = new T();
        socket.Init(ip, port, playerId, token, deserialize, socketevent);
    }
    public void Dispose()
    {
        socket?.Dispose();
    }
    public void Reconnect()
    {
        socket?.Reconnect();
    }
    public void Close()
    {
        socket?.Close();
    }
    public void Send(ushort id, ISerialize msg)
    {
        socket?.Send(id, msg);
    }
}