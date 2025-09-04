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
    /// 创建后会自动连接
    /// </summary>
    public void Create<T>(string ip, ushort port, uint connectId) where T : SBase, new()
    {
        socket = new T();
        socket.Init(ip, port, connectId, deserialize, socketevent);
    }
    /// <summary>
    /// 关闭后需要重新创建
    /// </summary>
    public void Close()
    {
        socket.Close();
    }
    public void Connect()
    {
        socket.Connect();
    }
    public void Send(ushort id, IExtensible msg)
    {
        socket.Send(id, msg);
    }
    public void SetHeartState(bool open)
    {
        socket.SetHeartState(open);
    }
}