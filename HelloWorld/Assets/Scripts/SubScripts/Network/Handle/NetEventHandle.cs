using System.Collections.Concurrent;

public class NetEventHandle
{
    struct SocketEventItem
    {
        public NetEvent type;
        public int param;
    }

    private ConcurrentQueue<SocketEventItem> socketevent = new ConcurrentQueue<SocketEventItem>();

    public NetEventHandle()
    {
        Driver.Instance.StartUpdate(Update);
    }
    public void Handle(NetEvent type, int param)
    {
        socketevent.Enqueue(new SocketEventItem { type = type, param = param });
    }
    private void Update(float t)
    {
        while (socketevent.TryDequeue(out var item))
        {
            switch (item.type)
            {
                case NetEvent.Connected:
                    AccountInfo.Instance.CSAuth();
                    NetClient.Instance.StartHeart();
                    UICommonTips.ShowTips("连接服务器成功");
                    break;
                case NetEvent.ConnectError:
                    NetClient.Instance.StopHeart();
                    UICommonBoxParam param = new UICommonBoxParam();
                    param.type = UICommonBoxType.Sure;
                    param.title = "网络异常";
                    param.content = "网络连接已断开，请检查网络设置";
                    param.sure = a => NetClient.Instance.Connect();
                    UICommonBox.OpenCommonBox(param);
                    break;
                case NetEvent.InvalidToken:
                    NetClient.Instance.StopHeart();
                    AccountInfo.Instance.RefreshAuth();
                    UICommonTips.ShowTips("鉴权失败");
                    break;
            }
        }
    }
}
