using ProtoBuf;
using System.Collections.Generic;

namespace HotAssembly
{
    public enum SOType
    {
        Main,
        Fight,
    }
    public class SocketManager
    {
        private Dictionary<SOType, SocketObject> sos = new Dictionary<SOType, SocketObject>();
        public void Init()
        {
            //Create(SOType.Main);
        }
        public void Create(SOType st, string ip, ushort port)
        {
            if (sos.ContainsKey(st)) return;
            SocketObject so = new SocketObject();
            so.Init(ip, port);
            sos.Add(st, so);
        }
        public void Send(NetMsgId id, IExtensible msg, SOType st = SOType.Main)
        {
            sos[st].Send((ushort)id, msg);
        }
    }
}

