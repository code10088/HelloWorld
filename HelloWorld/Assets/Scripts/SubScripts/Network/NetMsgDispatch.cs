namespace HotAssembly
{
    public partial class NetMsgDispatch : Singletion<NetMsgDispatch>
    {
        public void Init()
        {
            global::NetMsgDispatch.Instance.dispatch = Dispatch;
            global::NetMsgDispatch.Instance.deserialize = Deserialize;
        }
        private void Dispatch(NetMsgItem msg)
        {
            switch (msg.id)
            {
                case NetMsgId.Min:; break;
            }
        }
    }
}
