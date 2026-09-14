﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class NetMsgDispatch : Singleton<NetMsgDispatch>
{
    struct NetMsgItem
    {
        public ushort id;
        public IDeserialize msg;
    }
    struct SocketEventItem
    {
        public int type;
        public int param;
    }
    private Queue<NetMsgItem> msgPool1 = new Queue<NetMsgItem>();
    private Queue<NetMsgItem> msgPool2 = new Queue<NetMsgItem>();
    private object msgLock = new object();
    private Dictionary<ushort, Action<IDeserialize>> msgAction = new Dictionary<ushort, Action<IDeserialize>>();
    private ConcurrentQueue<SocketEventItem> socketevent = new ConcurrentQueue<SocketEventItem>();

    public void Init()
    {
        SocketManager.Instance.SetFunc(Deserialize, HandleSocketEvent);
        Driver.Instance.StartUpdate(Update);
    }
    public void Register(ushort id, Action<IDeserialize> action)
    {
        msgAction[id] = action;
    }
    public void UnRegister(ushort id)
    {
        msgAction.Remove(id);
    }
    private void HandleSocketEvent(int type, int param)
    {
        socketevent.Enqueue(new SocketEventItem { type = type, param = param });
    }
    private void HandleMsg(ushort id, IDeserialize msg)
    {
        lock (msgLock) msgPool1.Enqueue(new NetMsgItem { id = id, msg = msg });
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
        while (socketevent.TryDequeue(out var item))
        {
            HandleSocketEvent(item);
        }
    }

    private void HandleSocketEvent(SocketEventItem item)
    {
        switch ((SocketEvent)item.type)
        {
            case SocketEvent.Reconect:
                UICommonTips.ShowTips("尝试连接服务器");
                break;
            case SocketEvent.Connected:
                UICommonTips.ShowTips("连接服务器成功");
                EventManager.Instance.Fire(EventType.NetworkConnected);
                break;
            case SocketEvent.ConnectError:
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "网络异常";
                param.content = "网络连接已断开，请检查网络设置";
                param.sure = a => SocketManager.Instance.Reconnect();
                UICommonBox.OpenCommonBox(param);
                break;
            case SocketEvent.RefreshDelay:
                EventManager.Instance.Fire(EventType.RefreshDelay, item.param);
                break;
        }
    }
}
