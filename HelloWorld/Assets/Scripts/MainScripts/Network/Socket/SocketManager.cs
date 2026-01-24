using ProtoBuf;
using System;

public class SocketManager : Singletion<SocketManager>
{
    private SBase socket;
    private Func<ushort, Memory<byte>, bool> deserialize;
    private Action<int, int> socketevent;

    public void SetFunc(Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
    }
    /// <summary>
    /// 创建自动连接
    /// </summary>
    public void Create<T>(string ip, ushort port, uint playerId, string token) where T : SBase, new()
    {
        socket = new T();
        socket.Init(ip, port, playerId, token, deserialize, socketevent);
    }
    public void Close()
    {
        socket.Close();
    }
    public void Reconnect()
    {
        socket.Reconnect();
    }
    public void Send(ushort id, IExtensible msg)
    {
        socket?.Send(id, msg);
    }
}