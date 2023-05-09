using ProtoBuf;
using System.Collections.Generic;

namespace HotAssembly
{
    public partial class NetMsgDispatch : Singletion<NetMsgDispatch>
    {
        class NetMsgItem
        {
            public ushort id;
            public IExtensible msg;
        }

        private Queue<NetMsgItem> msgPool = new Queue<NetMsgItem>();

        public void Init()
        {
            UpdateManager.Instance.StartUpdate(Update);
        }
        private void Update()
        {
            while(msgPool.Count > 0)
            {
                lock (msgPool)
                {
                    NetMsgItem msg = msgPool.Dequeue();
                    Dispatch(msg);
                }
            }
        }
        private void Dispatch(NetMsgItem msg)
        {
            switch (msg.id)
            {
                case NetMsgId.Min:; break;
            }
        }
    }
}
