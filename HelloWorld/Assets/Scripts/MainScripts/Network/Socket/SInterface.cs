using ProtoBuf;
using System;

public interface SInterface
{
    public void Init(string ip, ushort port, uint connectId, Func<byte[], bool> deserialize);
    public void Disconnect();
    public void Send(ushort id, IExtensible msg);
}