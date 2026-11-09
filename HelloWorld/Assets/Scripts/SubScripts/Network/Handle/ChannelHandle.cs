public class ChannelHandle : IChannelHandle
{
    private NetEventHandle eventHandle;

    public string Token => AccountInfo.Instance.AccessToken;

    public ChannelHandle()
    {
        eventHandle = new NetEventHandle();
    }

    public bool Deserialize(UnsafeByteBuffer buffer)
    {
        return NetMsgHandle.Instance.Deserialize(buffer);
    }

    public void HandleEvent(NetEvent type, int param)
    {
        eventHandle.Handle(type, param);
    }
}
