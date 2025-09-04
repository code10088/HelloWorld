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
    /// ��������Զ�����
    /// </summary>
    public void Create<T>(string ip, ushort port, uint connectId) where T : SBase, new()
    {
        socket = new T();
        socket.Init(ip, port, connectId, deserialize, socketevent);
    }
    /// <summary>
    /// �رպ���Ҫ���´���
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