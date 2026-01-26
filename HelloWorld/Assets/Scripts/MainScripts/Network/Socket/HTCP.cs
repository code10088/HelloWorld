using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class HTCP : SBase
{
    private Thread sendThread;
    private Thread receiveThread;

    #region 连接
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket.Connect(SocketType.Stream, ProtocolType.Tcp);
        if (socket.Connected)
        {
            socketevent.Invoke((int)SocketEvent.Connected, 0);
            connectMark = true;
            connectRetry = 0;
            sendRetry = 0;
            receiveRetry = 0;
            sendThread = new Thread(Send);
            sendThread.Start();
            receiveThread = new Thread(Receive);
            receiveThread.Start();
            heart.Start();
            return true;
        }
        else
        {
            Connect();
            return false;
        }
    }
    public override async Task Close()
    {
        await base.Close();
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
        serialize.Dispose();
    }
    #endregion

    #region 发送
    private void Send()
    {
        while (true)
        {
            if (connectMark == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var stream = serialize.Serialize(item.Id, item.Msg);
                while (true)
                {
                    int count = socket.Send(stream.Buffer, stream.WPos);
                    if (connectMark == false)
                    {
                        stream.Dispose();
                        return;
                    }
                    if (count == stream.WPos)
                    {
                        stream.Dispose();
                        sendRetry = 0;
                        break;
                    }
                    if (sendRetry++ > 0)
                    {
                        stream.Dispose();
                        Connect();
                        return;
                    }
                }
            }
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }
    #endregion

    #region 接收
    private void Receive()
    {
        while (true)
        {
            int count = socket.Receive(receiveBuffer);
            if (connectMark == false)
            {
                return;
            }
            if (count >= 0 && serialize.Deserialize(receiveBuffer, count))
            {
                receiveRetry = 0;
                Thread.Sleep(GameSetting.updateTimeSliceMS);
                continue;
            }
            if (receiveRetry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    #endregion
}