public class ChannelHandle : IChannelHandle
{
    public string Token => AccountInfo.Instance.AccessToken;

    public bool Deserialize(UnsafeByteBuffer buffer)
    {
        return NetClient.Instance.Deserialize(buffer);
    }
    public void HandleEvent(NetEvent type, int param)
    {
        NetClient.Instance.HandleEvent(type, param);
    }
}
