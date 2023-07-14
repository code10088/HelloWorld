using ProtoBuf;
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
    }

    private Dictionary<SType, STCP> tcp = new Dictionary<SType, STCP>();
    private Dictionary<SType, SKCP> kcp = new Dictionary<SType, SKCP>();

    public void Create(SType st, string ip, ushort port)
    {
        if (st < SType.T2K)
        {
            if (tcp.ContainsKey(st)) return;
            STCP so = new STCP();
            so.Init(ip, port);
            tcp.Add(st, so);
        }
        else
        {
            if (kcp.ContainsKey(st)) return;
            SKCP so = new SKCP();
            so.Init(ip, port);
            kcp.Add(st, so);
        }
    }
    public void Send(ushort id, IExtensible msg, SType st = SType.MainK)
    {
        if (st < SType.T2K) tcp[st].Send(id, msg);
        else kcp[st].Send(id, msg);        
    }
}

