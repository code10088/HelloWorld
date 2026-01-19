using ProtoBuf;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SKCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private uint connectId;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task updateTask;
    private Task receiveTask;
    private ConcurrentQueue<KcpPacket> queue = new ConcurrentQueue<KcpPacket>();

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        base.Init(ip, port, connectId, deserialize, socketevent);
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        this.connectId = connectId;
        kcpSend = new KcpSend(Send);
        Connect();
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SendTimeout = heartInterval;
        socket.ReceiveTimeout = heartInterval;
        kcp = new PoolSegManager.Kcp(connectId, kcpSend);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();
        try
        {
            await socket.ConnectAsync(endPoint);
        }
        catch
        {

        }
        connectMark = true;
        sendRetry = 0;
        receiveRetry = 0;
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        sendTask = Send(cts.Token);
        updateTask = Update(cts.Token);
        receiveTask = Receive(cts.Token);
        socketevent.Invoke((int)SocketEvent.Connected, 0);
        return true;
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
        socket?.Close();
        socket?.Dispose();
        socket = null;
        kcp?.Dispose();
        kcp = null;
        await (sendTask ?? Task.CompletedTask);
        await (updateTask ?? Task.CompletedTask);
        await (receiveTask ?? Task.CompletedTask);
        while (queue.TryDequeue(out var item)) item.Dispose();
    }
    #endregion

    #region 发送
    class KcpSend : IKcpCallback
    {
        private Action<IMemoryOwner<byte>, int> Out;
        public KcpSend(Action<IMemoryOwner<byte>, int> _out)
        {
            Out = _out;
        }
        public void Output(IMemoryOwner<byte> owner, int avalidLength)
        {
            Out(owner, avalidLength);
        }
    }
    struct KcpPacket
    {
        private IMemoryOwner<byte> owner;
        private int length;
        public ReadOnlyMemory<byte> Datas => owner.Memory.Slice(0, length);
        public int Length => length;

        public KcpPacket(IMemoryOwner<byte> owner, int length)
        {
            this.owner = owner;
            this.length = length;
        }
        public void Dispose()
        {
            owner.Dispose();
        }
    }
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
                var wb = new WriteBuffer(bytePool, 256, 6);
                Serializer.Serialize(wb, item.msg);
                BinaryPrimitives.WriteInt32LittleEndian(wb.Buffer.AsSpan(0, 4), wb.Pos - 4);
                BinaryPrimitives.WriteUInt16LittleEndian(wb.Buffer.AsSpan(4, 2), item.id);
                kcp.Send(wb.Buffer.AsSpan(0, wb.Pos));
                wb.Dispose();
            }
            while (queue.TryDequeue(out var item))
            {
                while (true)
                {
                    int count = 0;
                    try
                    {
                        //socket使用CancellationToken，cts?.Cancel导致await无法退出
                        count = await socket.SendAsync(item.Datas, SocketFlags.None);
                    }
                    catch
                    {
                        count = -1;
                    }
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
        DateTimeOffset next = DateTime.UtcNow;
        while (true)
        {
            var current = DateTime.UtcNow;
            if (current >= next)
            {
                kcp.Update(current);
                next = kcp.Check(current);
            }
            var ms = (next - current).TotalMilliseconds;
            var delay = (int)Math.Clamp(ms, 1, 100);
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
        if (connectMark)
        {
            queue.Enqueue(new KcpPacket(owner, length));
            signal?.Release();
        }
        else
        {
            owner.Dispose();
        }
    }
    #endregion

    #region 接收
    private async Task Receive(CancellationToken token)
    {
        while (true)
        {
            int count = 0;
            try
            {
                //socket使用CancellationToken，cts?.Cancel导致await无法退出
                count = await socket.ReceiveAsync(receiveBuffer.AsMemory(), SocketFlags.None);
            }
            catch
            {
                count = -1;
            }
            if (connectMark == false)
            {
                return;
            }
            if (count >= 0 && Deserialize(count))
            {
                connectRetry = 0;
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
    private bool Deserialize(int receiveLength)
    {
        kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) break;
            var temp = bytePool.Rent(size);
            size = kcp.Recv(temp);
            if (size <= 0)
            {
                bytePool.Return(temp);
                break;
            }
            else
            {
                bool b = Deserialize(temp, size);
                bytePool.Return(temp);
                if (!b) return false;
            }
        }
        return true;
    }
    #endregion
}