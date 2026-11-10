using System;

public class NetClient : Singleton<NetClient>
{
    private IChannel channel;
    private ChannelHandle handle = new ChannelHandle();
    private NetEventHandle eventHandle = new NetEventHandle();
    private NetMsgHandle msgHandle = new NetMsgHandle();
    private HeartHandle heartHandle = new HeartHandle();

    public ConnectState State => channel?.State ?? ConnectState.Idle;

    #region 连接
    public void Create<T>(string ip, ushort port) where T : IChannel, new()
    {
        channel = new T();
        channel.Init(ip, port, handle);
    }
    public void Connect()
    {
        channel?.Connect();
    }
    public void Send(ushort id, ISerialize msg)
    {
        channel?.Send(id, msg);
        heartHandle.RefreshDelay1(id);
    }
    public void Close()
    {
        channel?.Close();
    }
    public void Dispose()
    {
        channel?.Dispose();
    }
    #endregion

    #region 接口
    public void HandleEvent(NetEvent type, int param) => eventHandle.Handle(type, param);
    public void Register(ushort id, Action<IDeserialize> action) => msgHandle.Register(id, action);
    public void Unregister(ushort id) => msgHandle.Unregister(id);
    public bool Deserialize(UnsafeByteBuffer buffer) => msgHandle.Deserialize(buffer);
    public void StartHeart() => heartHandle.Start(channel);
    public void StopHeart() => heartHandle.Stop();
    public void RefreshDelay(ushort id) => heartHandle.RefreshDelay2(id);
    #endregion
}