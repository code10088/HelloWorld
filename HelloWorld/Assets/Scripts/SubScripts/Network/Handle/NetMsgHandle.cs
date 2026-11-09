using Message;
using System;
using System.Collections.Generic;

public partial class NetMsgHandle : Singleton<NetMsgHandle>, SingletonInterface
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

    public void Init()
    {
        Driver.Instance.StartUpdate(Update);
        Preregister();
    }
    public void Register(ushort id, Action<IDeserialize> action)
    {
        msgAction[id] = action;
    }
    public void UnRegister(ushort id)
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
            NetMsgItem msg = msgPool2.Dequeue();
            if (msgAction.TryGetValue(msg.id, out var action)) action?.Invoke(msg.msg);
        }
    }

    private void Preregister()
    {
        Register(NetMsgId.Message_SCHeart, SCHeart);
        Register(NetMsgId.Message_SCPlayerData, SCPlayerInfo);
    }
    private void SCHeart(IDeserialize msg)
    {
        var heart = (SCHeart)msg;
        ServerTime.Instance.SetTime(heart.time);
    }
    private void SCPlayerInfo(IDeserialize msg)
    {
        DataManager.Instance.PlayerData.SCPlayerInfo(msg);
    }
}
