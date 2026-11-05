public interface ChannelInterface
{
    public ConnectState State { get; }
    public void Init(string ip, ushort port, IDispatch dispatch);
    public void Connect();
    public void Close();
    public void Dispose();
    public void Send(ushort id, ISerialize msg);
}
