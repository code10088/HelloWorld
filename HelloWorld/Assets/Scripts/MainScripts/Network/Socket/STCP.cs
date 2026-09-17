#if !UNITY_WEBGL
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task receiveTask;
    private UnsafeByteBuffer headBuffer;
    private UnsafeByteBuffer bodyBuffer;
    private int headLength = 4;
    private int bodyLength = 0;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        headBuffer = UnsafeByteBuffer.Rent(4);
        bodyBuffer = UnsafeByteBuffer.Rent(2048);
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override async Task ConnectTask()
    {
        if (connectRetry++ > 0)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        if (socket.Connect(SocketType.Stream, ProtocolType.Tcp))
        {
            signal = new SemaphoreSlim(0);
            cts = new CancellationTokenSource();
            State = ConnectState.Connected;
            connectRetry = 0;
            sendTask = Send(cts.Token);
            receiveTask = Receive(cts.Token);
            heart.Start();
            socketevent.Invoke((int)SocketEvent.Connected, 0);
        }
        else
        {
            Connect();
        }
    }
    protected override async Task CloseTask()
    {
        cts?.Cancel();
        signal?.Release();
        await base.CloseTask();
        await Task.WhenAll(sendTask ?? Task.CompletedTask, receiveTask ?? Task.CompletedTask);
        cts?.Dispose();
        signal?.Dispose();
        cts = null;
        signal = null;
        sendTask = null;
        receiveTask = null;
        headBuffer?.Clear();
        bodyBuffer?.Clear();
        bodyLength = 0;
    }
    protected override async Task DisposeTask()
    {
        await base.DisposeTask();
        UnsafeByteBuffer.Return(headBuffer);
        headBuffer = null;
        UnsafeByteBuffer.Return(bodyBuffer);
        bodyBuffer = null;
    }
    #endregion

    #region 发送
    public override void Send(ushort id, ISerialize msg)
    {
        if (State == ConnectState.Connected)
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
                await signal.WaitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
            if (State != ConnectState.Connected)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                item.Serialize(sendBuffer, true);
                int length = sendBuffer.WPos;
                int count = await socket.SendAsync1(sendBuffer.Mem, token).ConfigureAwait(false);
                if (State != ConnectState.Connected)
                {
                    return;
                }
                if (count != length)
                {
                    Connect();
                    return;
                }
            }
        }
    }
    #endregion

    #region 接收
    private async Task Receive(CancellationToken token)
    {
        int retry = 0;
        while (true)
        {
            int count = await socket.ReceiveAsync(receiveBuffer.Memory, token).ConfigureAwait(false);
            if (State != ConnectState.Connected)
            {
                return;
            }
            if (count > 0 && Deserialize(receiveBuffer, count))
            {
                retry = 0;
                continue;
            }
            if (count == 0 || retry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    private bool Deserialize(UnsafeByteBuffer buffer, int length)
    {
        int pos = 0;
        while (pos < length)
        {
            if (headBuffer.WPos < headLength)
            {
                int l = Math.Min(headLength - headBuffer.WPos, length - pos);
                headBuffer.WriteBuffer(buffer, pos, l);
                pos += l;
                if (headBuffer.WPos == headLength)
                {
                    headBuffer.SetRPos(0);
                    bodyLength = headBuffer.ReadInt();
                    if (bodyLength < 2 || bodyLength > bodyBuffer.Capacity)
                    {
                        headBuffer.Clear();
                        bodyBuffer.Clear();
                        bodyLength = 0;
                        return false;
                    }
                }
            }
            else
            {
                int l = Math.Min(bodyLength - bodyBuffer.WPos, length - pos);
                bodyBuffer.WriteBuffer(buffer, pos, l);
                pos += l;
                if (bodyBuffer.WPos == bodyLength)
                {
                    bool b = Receive(bodyBuffer);
                    headBuffer.Clear();
                    bodyBuffer.Clear();
                    bodyLength = 0;
                    if (!b) return false;
                }
            }
        }
        return true;
    }
    #endregion
}
#endif
