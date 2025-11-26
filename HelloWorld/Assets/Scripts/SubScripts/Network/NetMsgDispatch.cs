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
        Driver.Instance.StartUpdate(Update);
    }
    public void Register(ushort id, Action<IExtensible> action)
    {
        msgAction[id] = action;
    }
    private void HandleSocketEvent(int type, int param)
    {
        socketevent.Enqueue(new SocketEventItem() { type = type, param = param });
    }
    private void HandleMsg(ushort id, IExtensible msg)
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
            var item = socketevent.Dequeue();
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
                break;
            case SocketEvent.ConnectError:
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "网络异常";
                param.content = "网络连接已断开，请检查网络设置";
                param.sure = a => SocketManager.Instance.Connect();
                UICommonBox.OpenCommonBox(param);
                break;
            case SocketEvent.RefreshDelay:
                EventManager.Instance.FireEvent(EventType.RefreshDelay, item.param);
                break;
        }
    }
}