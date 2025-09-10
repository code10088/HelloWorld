using ProtoBuf;
using System;
using System.Collections.Generic;

public partial class NetMsgDispatch : Singletion<NetMsgDispatch>
{
    class NetMsgItem
    {
        public ushort id;
        public IExtensible msg;
    }
    class SocketEventItem
    {
        public int type;
        public int param;
    }
    private Queue<NetMsgItem> msgPool = new Queue<NetMsgItem>();
    private Dictionary<ushort, Action<IExtensible>> msgAction = new Dictionary<ushort, Action<IExtensible>>();
    private Queue<SocketEventItem> socketevent = new Queue<SocketEventItem>();

    public void Init()
    {
        SocketManager.Instance.SetFunc(Deserialize, HandleSocketEvent);
        Updater.Instance.StartUpdate(Update);
    }
    public void Register(ushort id, Action<IExtensible> action)
    {
        msgAction[id] = action;
    }
    private void HandleSocketEvent(int type, int param)
    {
        socketevent.Enqueue(new SocketEventItem() { type = type, param = param });
    }
    private void Add(ushort id, IExtensible msg)
    {
        lock (msgPool) msgPool.Enqueue(new NetMsgItem() { id = id, msg = msg });
    }
    private void Update(float t)
    {
        while (msgPool.Count > 0)
        {
            lock (msgPool)
            {
                NetMsgItem msg = msgPool.Dequeue();
                if (msgAction.TryGetValue(msg.id, out var action)) action?.Invoke(msg.msg);
            }
        }
        while (socketevent.Count > 0)
        {
            var temp = socketevent.Dequeue();
            switch ((SocketEvent)temp.type)
            {
                case SocketEvent.ConnectError:
                    OnConnectError();
                    break;
                case SocketEvent.RefreshDelay:
                    OnRefreshDelay(temp.param);
                    break;
            }
        }
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
}