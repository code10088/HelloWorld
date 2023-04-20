using System;

namespace MainAssembly
{
    public class NetMessageBase
    {
        private SocketObject so;
        private int sendTimer;
        private int retryTime;
        private byte[] Serialize()
        {
            //TODO protobuf Serialize
            return null;
        }
        public void Send(SocketObject so)
        {
            this.so = so;
            byte[] bytes = Serialize();
            so.BeginSend(bytes, SendCallback);
            sendTimer = TimeManager.Instance.StartTimer(10, finish: () => SendCallback(false));
        }
        private void SendCallback(IAsyncResult ar)
        {
            TimeManager.Instance.StopTimer(sendTimer);
            int sendLength = so.EndSend(ar);
            SendCallback(sendLength > 0);
        }
        private void SendCallback(bool result)
        {
            if (result)
            {
                retryTime = 0;
                so.Send(true, this);
            }
            else if (retryTime < 3)
            {
                retryTime++;
                so.Send(false, this);
            }
            else
            {
                retryTime = 0;
                GameDebug.LogError("消息发送失败:" + this);
            }
        }
    }
}
