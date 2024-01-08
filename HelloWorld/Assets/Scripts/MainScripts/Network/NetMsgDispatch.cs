using ProtoBuf;
using System;
using System.Collections.Generic;

public class NetMsgDispatch : Singletion<NetMsgDispatch>
{
    private Queue<NetMsgItem> msgPool = new Queue<NetMsgItem>();
    public Action<NetMsgItem> dispatch;
    public Func<byte[], bool> deserialize;

    public void Init()
    {
        Updater.Instance.StartUpdate(Update);
    }
    private void Update()
    {
        while (msgPool.Count > 0)
        {
            lock (msgPool)
            {
                NetMsgItem msg = msgPool.Dequeue();
                dispatch?.Invoke(msg);
            }
        }
    }
    public bool Deserialize(byte[] bytes)
    {
        bool? b = deserialize?.Invoke(bytes);
        return b.HasValue && b.Value;
    }
    public void Add(NetMsgItem item)
    {
        lock (msgPool) msgPool.Enqueue(item);
    }
}
public class NetMsgItem
{
    public ushort id;
    public IExtensible msg;
}