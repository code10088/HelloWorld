using System.Collections.Generic;

namespace MainAssembly
{
    public enum SOType
    {
        Main,
        Fight,
    }
    public class SocketManager : Singletion<SocketManager>
    {
        private Dictionary<SOType, SocketObject> sos = new Dictionary<SOType, SocketObject>();

        public void Create(SOType st, string ip, ushort port)
        {
            if (!sos.ContainsKey(st))
            {
                SocketObject so = new SocketObject();
                so.Init(ip, port);
                sos.Add(st, so);
            }
        }
        public void Send(NetMessageBase nmb, SOType st = SOType.Main)
        {
            sos[st].Send(nmb);
        }
    }
}

