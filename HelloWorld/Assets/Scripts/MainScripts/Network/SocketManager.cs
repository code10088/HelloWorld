using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

public class SocketManager : Singletion<SocketManager>
{
    public enum SOType
    {
        Main,
        Fight,
    }

    private Dictionary<SOType, SocketObject> sos = new Dictionary<SOType, SocketObject>();
    private Action<ushort, MemoryStream> dispatch;
    public void Init(Action<ushort, MemoryStream> dispatch)
    {
        this.dispatch = dispatch;
    }
    public void Create(SOType st, string ip, ushort port)
    {
        if (sos.ContainsKey(st)) return;
        SocketObject so = new SocketObject();
        so.Init(ip, port, dispatch);
        sos.Add(st, so);
    }
    public void Send(ushort id, IExtensible msg, SOType st = SOType.Main)
    {
        sos[st].Send(id, msg);
    }
}

