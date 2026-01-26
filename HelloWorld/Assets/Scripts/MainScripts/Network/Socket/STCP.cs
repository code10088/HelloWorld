using ProtoBuf;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task receiveTask;

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
            signal = new SemaphoreSlim(0);
            cts = new CancellationTokenSource();
            sendTask = Send(cts.Token);
            receiveTask = Receive(cts.Token);
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
        signal?.Release();
        signal?.Dispose();
        signal = null;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        await (sendTask ?? Task.CompletedTask);
        await (receiveTask ?? Task.CompletedTask);
        serialize.Dispose();
    }
    #endregion

    #region 发送
    public override void Send(ushort id, IExtensible msg)
    {
        if (connectMark)
        {
            base.Send(id, msg);
            signal?.Release();
        }
    }
    private async Task Send(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await signal.WaitAsync(token);
            }
            catch
            {
                return;
            }
            if (connectMark == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var stream = serialize.Serialize(item.Id, item.Msg);
                while (true)
                {
                    int count = await socket.SendAsync(stream.Buffer, stream.WPos);
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
        }
    }
    #endregion

    #region 接收
    private async Task Receive(CancellationToken token)
    {
        while (true)
        {
            int count = await socket.ReceiveAsync(receiveBuffer);
            if (connectMark == false)
            {
                return;
            }
            if (count >= 0 && serialize.Deserialize(receiveBuffer, count))
            {
                receiveRetry = 0;
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