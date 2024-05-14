using ProtoBuf;
using System;
using System.Collections.Generic;

public class SocketManager : Singletion<SocketManager>
{
    public enum SType
    {
        MainT,
        FightT,
        T2K,
        MainK,
        FightK,
        K2Web,
        MainWeb,
        FightWeb,
    }

    private Dictionary<SType, STCP> tcp = new Dictionary<SType, STCP>();
    private Dictionary<SType, SKCP> kcp = new Dictionary<SType, SKCP>();
    private Dictionary<SType, SWeb> web = new Dictionary<SType, SWeb>();
    private Func<byte[], bool> deserialize;

    public void SetDeserialize(Func<byte[], bool> deserialize)
    {
        this.deserialize = deserialize;
    }
    public void Create(SType st, string ip, ushort port, uint connectId = 0)
    {
        if (st < SType.T2K)
        {
            if (tcp.ContainsKey(st)) return;
            STCP so = new STCP();
            so.Init(ip, port);
            tcp.Add(st, so);
        }
        else if (st < SType.K2Web)
        {
            if (kcp.ContainsKey(st)) return;
            SKCP so = new SKCP();
            so.Init(ip, port, connectId);
            kcp.Add(st, so);
        }
        else
        {
            if (kcp.ContainsKey(st)) return;
            SWeb so = new SWeb();
            so.Init(ip, port);
            web.Add(st, so);
        }
    }
    public void Send(ushort id, IExtensible msg, SType st = SType.MainK)
    {
        if (st < SType.T2K) tcp[st].Send(id, msg);
        else if (st < SType.K2Web) kcp[st].Send(id, msg);
        else web[st].Send(id, msg);
    }
    public bool Deserialize(byte[] bytes)
    {
        return deserialize.Invoke(bytes);
    }
}