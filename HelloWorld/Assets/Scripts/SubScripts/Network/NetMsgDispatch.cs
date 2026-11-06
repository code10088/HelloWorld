using Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class NetMsgDispatch : Singleton<NetMsgDispatch>, SingletonInterface, IDispatch
{
    struct NetMsgItem
    {
        public ushort id;
        public IDeserialize msg;
    }
    struct SocketEventItem
    {
        public NetEvent type;
        public int param;
    }
    public string Token => AccountInfo.Instance.AccessToken;
    private Queue<NetMsgItem> msgPool1 = new Queue<NetMsgItem>();
    private Queue<NetMsgItem> msgPool2 = new Queue<NetMsgItem>();
    private object msgLock = new object();
    private Dictionary<ushort, Action<IDeserialize>> msgAction = new Dictionary<ushort, Action<IDeserialize>>();
    private ConcurrentQueue<SocketEventItem> socketevent = new ConcurrentQueue<SocketEventItem>();

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
    public void HandleNetEvent(NetEvent type, int param)
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
        switch (item.type)
        {
            case NetEvent.Reconnect:
                UICommonTips.ShowTips("尝试连接服务器");
                break;
            case NetEvent.Connected:
                UICommonTips.ShowTips("连接服务器成功");
                EventManager.Instance.Fire(EventType.NetworkConnected);
                break;
            case NetEvent.ConnectError:
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "网络异常";
                param.content = "网络连接已断开，请检查网络设置";
                param.sure = a => NetClient.Instance.Connect();
                UICommonBox.OpenCommonBox(param);
                break;
            case NetEvent.InvalidToken:
                UICommonTips.ShowTips("鉴权失败");
                AccountInfo.Instance.RefreshAuth();
                break;
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
