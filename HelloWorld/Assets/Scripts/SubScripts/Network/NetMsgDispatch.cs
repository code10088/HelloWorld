using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;

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
        SocketManager.Instance.SetFunc(Deserialize, HandleSocketEvent);
        Updater.Instance.StartUpdate(Update);
    }
    private void HandleSocketEvent(int type, int param)
    {
        var temp = (SocketEvent)type;
        switch (temp)
        {
            case SocketEvent.NetworkChange:
                OnNetworkChange();
                break;
            case SocketEvent.ConnectError:
                OnConnectError();
                break;
            case SocketEvent.RefreshDelay:
                OnRefreshDelay(param);
                break;
        }
    }
    private void OnNetworkChange()
    {
        var temp = Application.internetReachability;
        if (temp == NetworkReachability.NotReachable) OnConnectError();
        else EventManager.Instance.FireEvent(EventType.NetworkChange, temp);
    }
    private void OnConnectError()
    {
        UICommonBoxParam param = new UICommonBoxParam();
        param.type = UICommonBoxType.Sure;
        param.title = "网络异常";
        param.content = "网络连接已断开，请检查网络设置";
        param.sure = a => SocketManager.Instance.Connect();
        UICommonBox.OpenCommonBox(param);
    }
    private void OnRefreshDelay(int delay)
    {
        EventManager.Instance.FireEvent(EventType.RefreshDelay, delay);
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
        switch (msg.id)
        {
            case NetMsgId.ProtoTest_Person:; break;
        }
    }
}