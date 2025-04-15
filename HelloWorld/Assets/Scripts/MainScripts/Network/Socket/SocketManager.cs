using ProtoBuf;
using System;
using System.Collections.Generic;

public class SocketManager : Singletion<SocketManager>
{
    private int uniqueId = 0;
    private Dictionary<int, SInterface> socket = new Dictionary<int, SInterface>();
    private Func<byte[], bool> deserialize;

    public void SetDeserialize(Func<byte[], bool> deserialize)
    {
        this.deserialize = deserialize;
    }
    public int Create<T>(string ip, ushort port, uint connectId) where T : SInterface, new()
    {
        T s = new T();
        s.Init(ip, port, connectId, deserialize);
        socket.Add(uniqueId, s);
        return uniqueId++;
    }
    public void Disconnect(int id)
    {
        if (socket.TryGetValue(id, out SInterface s))
        {
            s.Disconnect();
            socket.Remove(id);
        }
    }
    public void Send(ushort id, IExtensible msg, int sid = 0)
    {
        socket[sid].Send(id, msg);
    }
    public bool Deserialize(byte[] bytes)
    {
        return deserialize.Invoke(bytes);
    }
}