#if !UNITY_WEBGL
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

public class KcpTransport : TransportBase
{
    private SocketHandle socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task updateTask;
    private Task receiveTask;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private DateTimeOffset next;
    private SendItem kcpConnect;
    private ConcurrentQueue<KcpPacket> queue = new ConcurrentQueue<KcpPacket>();

    public override void Init(string ip, ushort port, uint playerId, string token, IDispatch dispatch)
    {
        socket = new SocketHandle(ip, port);
        kcpSend = new KcpSend(Send);
        var msg = new CS_KcpConnect();
        msg.playerId = playerId;
        msg.token = token;
        kcpConnect = new SendItem(NetMsgId.CSKcpConnect, msg);
        base.Init(ip, port, playerId, token, dispatch);
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task ConnectTask()
    {
        if (connectRetry++ > 0)
        {
            dispatch.HandleSocketEvent(SocketEvent.ConnectError, 0);
            return;
        }
        dispatch.HandleSocketEvent(SocketEvent.Reconnect, 0);
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            dispatch.HandleSocketEvent(SocketEvent.ConnectError, 0);
            return;
        }
        if (socket.Connect(SocketType.Dgram, ProtocolType.Udp) == false)
        {
            Connect();
            return;
        }
        kcpConnect.Serialize(sendBuffer);
        int retry = 0;
        while (true)
        {
            int count = socket.Send(sendBuffer.Span);
            if (count == sendBuffer.WPos)
            {
                retry = 0;
                break;
            }
            if (retry++ > 0)
            {
                Connect();
                return;
            }
        }
        while (true)
        {
            int count = socket.Receive(receiveBuffer.FullSpan);
            if (count == 6)
            {
                receiveBuffer.SetWPos(6);
                receiveBuffer.SetRPos(0);
                if (receiveBuffer.ReadUShort() == NetMsgId.SCKcpConnect)
                {
                    var connectId = receiveBuffer.ReadUInt();
                    kcp = new PoolSegManager.Kcp(connectId, kcpSend);
                    kcp.NoDelay(1, 10, 2, 1);
                    kcp.WndSize();
                    kcp.SetMtu();
                    next = DateTime.UtcNow;

                    signal = new SemaphoreSlim(0);
                    cts = new CancellationTokenSource();
                    State = ConnectState.Connected;
                    connectRetry = 0;
                    sendTask = Send(cts.Token);
                    updateTask = Update(cts.Token);
                    receiveTask = Receive(cts.Token);
                    heart.Start();
                    dispatch.HandleSocketEvent(SocketEvent.Connected, 0);
                    return;
                }
            }
            if (count >= 0)
            {
                retry = 0;
                continue;
            }
            if (retry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    protected override async Task CloseTask()
    {
        cts?.Cancel();
        signal?.Release();
        socket?.Dispose();
        await base.CloseTask();
        await Task.WhenAll(sendTask ?? Task.CompletedTask, updateTask ?? Task.CompletedTask, receiveTask ?? Task.CompletedTask);
        cts?.Dispose();
        signal?.Dispose();
        cts = null;
        signal = null;
        sendTask = null;
        updateTask = null;
        receiveTask = null;
        while (queue.TryDequeue(out var item)) item.Dispose();
        kcp?.Dispose();
        kcp = null;
    }
    #endregion

    #region 发送
    public class KcpSend : IKcpCallback
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
                item.Serialize(sendBuffer);
                kcp.Send(sendBuffer.Span);
            }
            int retry = 0;
            while (queue.TryDequeue(out var item))
            {
                while (true)
                {
                    int count = await socket.SendAsync2(item.Datas, token).ConfigureAwait(false);
                    if (State != ConnectState.Connected)
                    {
                        item.Dispose();
                        return;
                    }
                    if (count == item.Length)
                    {
                        item.Dispose();
                        retry = 0;
                        break;
                    }
                    if (retry++ > 0)
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
            try
            {
                var current = DateTime.UtcNow;
                kcp.Update(current);
                next = kcp.Check(current);
                var ms = (next - current).TotalMilliseconds;
                var delay = (int)Math.Clamp(ms, 1, 10);
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
            if (State != ConnectState.Connected)
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
        if (State == ConnectState.Connected)
        {
            queue.Enqueue(new KcpPacket(owner, length));
            signal?.Release();
        }
        else
        {
            owner.Dispose();
        }
    }
    private struct KcpPacket
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
            if (count >= 0 && Deserialize(receiveBuffer, count))
            {
                retry = 0;
                continue;
            }
            if (retry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    private bool Deserialize(UnsafeByteBuffer buffer, int length)
    {
        try
        {
            kcp.Input(buffer.FullSpan.Slice(0, length));
            while (true)
            {
                int size = kcp.PeekSize();
                if (size <= 0) return true;
                size = kcp.Recv(buffer.FullSpan);
                if (size <= 0) return true;
                buffer.SetWPos(size);
                buffer.SetRPos(0);
                if (!Receive(buffer)) return false;
            }
        }
        catch
        {
            return false;
        }
    }
    #endregion
}
#endif
