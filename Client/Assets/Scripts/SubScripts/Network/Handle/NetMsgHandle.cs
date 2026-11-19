using System;
using System.Collections.Generic;

public partial class NetMsgHandle
{
    struct NetMsgItem
    {
        public ushort id;
        public IDeserialize msg;
    }

    private object msgLock = new object();
    private Queue<NetMsgItem> msgPool1 = new Queue<NetMsgItem>();
    private Queue<NetMsgItem> msgPool2 = new Queue<NetMsgItem>();
    private Dictionary<ushort, Action<IDeserialize>> msgAction = new Dictionary<ushort, Action<IDeserialize>>();

    public NetMsgHandle()
    {
        Driver.Instance.StartUpdate(Update);
    }
    public void Register(ushort id, Action<IDeserialize> action)
    {
        msgAction[id] = action;
    }
    public void Unregister(ushort id)
    {
        msgAction.Remove(id);
    }
    private void Handle(ushort id, IDeserialize msg)
    {
        lock (msgLock)
        {
            msgPool1.Enqueue(new NetMsgItem { id = id, msg = msg });
        }
    }
    private void Update(float t)
    {
        lock (msgLock)
        {
            var temp = msgPool2;
            msgPool2 = msgPool1;
            msgPool1 = temp;
        }
        while (msgPool2.Count > 0)
        {
            var msg = msgPool2.Dequeue();
            NetClient.Instance.RefreshDelay(msg.id);
            if (msgAction.TryGetValue(msg.id, out var action)) action?.Invoke(msg.msg);
        }
    }
}
