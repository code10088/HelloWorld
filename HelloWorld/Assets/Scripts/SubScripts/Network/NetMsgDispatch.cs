using ProtoBuf;
using System.Collections.Generic;

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
        SocketManager.Instance.SetDeserialize(Deserialize);
        Updater.Instance.StartUpdate(Update);
    }
    public void Add(ushort id, IExtensible msg)
    {
        lock (msgPool)
        {
            var item = new NetMsgItem();
            item.id = id;
            item.msg = msg;
            msgPool.Enqueue(item);
        }
    }
    private void Update(float t)
    {
        while (msgPool.Count > 0)
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
        switch ((MessageType)msg.id)
        {
            case MessageType.Person:; break;
        }
    }
}