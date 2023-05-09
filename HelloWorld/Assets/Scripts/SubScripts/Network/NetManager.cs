using System.IO;

namespace HotAssembly
{
    public class NetManager : Singletion<NetManager>
    {
        private SocketManager sm = new SocketManager();
        private NetMsgDispatch nmd = new NetMsgDispatch();

        public void Init()
        {
            sm.Init();
            nmd.Init();
        }
        public void Dispatch(ushort id, MemoryStream ms)
        {
            nmd.Deserialize(id, ms);
        }
    }
}
