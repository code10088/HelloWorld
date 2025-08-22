using ProtoBuf;
using System;

public class SocketManager : Singletion<SocketManager>
{
    private SInterface socket;
    private Func<byte[], bool> deserialize;

    public void SetDeserialize(Func<byte[], bool> deserialize)
    {
        this.deserialize = deserialize;
    }
    public void Create<T>(string ip, ushort port, uint connectId) where T : SInterface, new()
    {
        socket = new T();
        socket.Init(ip, port, connectId, deserialize);
    }
    public void Disconnect()
    {
        socket.Disconnect();
    }
    public void Send(ushort id, IExtensible msg)
    {
        socket.Send(id, msg);
    }
}