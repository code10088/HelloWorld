using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class SKCP : SBase
{
    private KcpHandle kcp;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task updateTask;
    private Task receiveTask;
    private ConcurrentQueue<KcpPacket> queue = new ConcurrentQueue<KcpPacket>();

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        kcp = new KcpHandle(playerId, token, Send, Receive);
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket.Connect(SocketType.Dgram, ProtocolType.Udp);
        var stream = serialize.Serialize(NetMsgId.CSKcpConnect, kcp.CS_KcpConnect);
        while (true)
        {
            //await不受socket超时影响会一直等待
            //int count = await socket.SendAsync(stream.Buffer.AsMemory(0, stream.Pos));
            int count = socket.Send(stream.Buffer.AsSpan(0, stream.WPos));
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
                return false;
            }
        }
        while (true)
        {
            //await不受socket超时影响会一直等待
            //int count = await socket.ReceiveAsync(receiveBuffer);
            int count = socket.Receive(receiveBuffer);
            if (count == 6 && BitConverter.ToUInt16(receiveBuffer) == NetMsgId.SCKcpConnect)
            {
                socketevent.Invoke((int)SocketEvent.Connected, 0);
                var connectId = BitConverter.ToUInt32(receiveBuffer, 2);
                kcp.Start(connectId);
                connectMark = true;
                connectRetry = 0;
                sendRetry = 0;
                receiveRetry = 0;
                cts = new CancellationTokenSource();
                sendTask = Send(cts.Token);
                updateTask = Update(cts.Token);
                receiveTask = Receive(cts.Token);
                heart.Start();
                return true;
            }
            if (count >= 0)
            {
                receiveRetry = 0;
                continue;
            }
            if (receiveRetry++ > 0)
            {
                Connect();
                return false;
            }
        }
    }
    public override async Task Close()
    {
        await base.Close();
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        kcp.Dispose();
        await (sendTask ?? Task.CompletedTask);
        await (updateTask ?? Task.CompletedTask);
        await (receiveTask ?? Task.CompletedTask);
        while (queue.TryDequeue(out var item)) item.Dispose();
    }
    #endregion

    #region 发送
    private async Task Send(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await Task.Delay(1, token);
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
                kcp.Send(stream.Buffer.AsSpan(0, stream.WPos));
                stream.Dispose();
            }
            while (queue.TryDequeue(out var item))
            {
                while (true)
                {
                    int count = await socket.SendAsync(item.Datas);
                    if (connectMark == false)
                    {
                        item.Dispose();
                        return;
                    }
                    if (count == item.Length)
                    {
                        item.Dispose();
                        sendRetry = 0;
                        break;
                    }
                    if (sendRetry++ > 0)
                    {
                        item.Dispose();
                        Connect();
                        return;
                    }
                }
            }
        }
    }
    private async Task Update(CancellationToken token)
    {
        while (true)
        {
            var ms = kcp.Update();
            var delay = (int)Math.Clamp(ms, 1, GameSetting.updateTimeSliceMS);
            try
            {
                await Task.Delay(delay, token);
            }
            catch
            {
                return;
            }
            if (connectMark == false)
            {
                return;
            }
        }
    }
    /// <summary>
    /// kcp.Update中执行
    /// </summary>
    private void Send(IMemoryOwner<byte> owner, int length)
    {
        if (connectMark) queue.Enqueue(new KcpPacket(owner, length));
        else owner.Dispose();
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
            if (count >= 0 && kcp.Deserialize(receiveBuffer, count))
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