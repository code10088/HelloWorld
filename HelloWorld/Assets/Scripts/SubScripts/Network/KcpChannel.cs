using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

public class KcpChannel : ChannelInterface
{
    private string ip;
    private ushort port;
    private IDispatch dispatch;
    private int version;
    private SocketHandle socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private ConcurrentQueue<SendItem> sendQueue1;
    private ConcurrentQueue<KcpPacket> sendQueue2;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private DateTimeOffset next;
    private int state = (int)ConnectState.Idle;

    public ConnectState State => (ConnectState)Volatile.Read(ref state);

    #region 连接
    public void Init(string ip, ushort port, IDispatch dispatch)
    {
        this.ip = ip;
        this.port = port;
        this.dispatch = dispatch;
    }
    public void Connect()
    {
        Close();
        var v = Volatile.Read(ref version);
        Task.Run(() => ConnectTask(v));
    }
    private async Task ConnectTask(int v)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
            return;
        }
        var socket = new SocketHandle(ip, port);
        if (!socket.Connect(SocketType.Dgram, ProtocolType.Udp))
        {
            socket?.Dispose();
            dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
            return;
        }
        var result = Handshake(v, socket, out uint conv);
        switch (result)
        {
            case 0:
                this.socket = socket;
                signal = new SemaphoreSlim(0);
                cts = new CancellationTokenSource();
                sendQueue1 = new ConcurrentQueue<SendItem>();
                sendQueue2 = new ConcurrentQueue<KcpPacket>();
                kcpSend = new KcpSend(Output);
                kcp = new PoolSegManager.Kcp(conv, kcpSend);
                kcp.NoDelay(1, 10, 2, 1);
                kcp.WndSize();
                kcp.SetMtu();
                next = DateTime.UtcNow;
                _ = SendTask(v, cts.Token);
                _ = UpdateTask(v, cts.Token);
                _ = RecvTask(v, cts.Token);
                Volatile.Write(ref state, (int)ConnectState.Connected);
                dispatch.HandleNetEvent(NetEvent.Connected, 0);
                break;
            case 1:
                socket?.Dispose();
                break;
            case 2:
                socket?.Dispose();
                dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
                break;
            case 3:
                socket?.Dispose();
                dispatch.HandleNetEvent(NetEvent.InvalidToken, 0);
                break;
        }
    }
    private int Handshake(int v, SocketHandle socket, out uint conv)
    {
        conv = 0;
        var sw = Stopwatch.StartNew();
        var stamp = sw.ElapsedMilliseconds;
        var send = UnsafeByteBuffer.Rent(2048);
        var recv = UnsafeByteBuffer.Rent(2048);
        send.SetWPos(0);
        send.WriteUShort(NetMsgId.Message_CSSession);
        send.WriteString(dispatch.Token);
        send.WriteInt(v);
        while (true)
        {
            if (Volatile.Read(ref version) != v)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                return 1;
            }
            if (sw.ElapsedMilliseconds >= stamp)
            {
                if (conv++ < 6)
                {
                    socket.Send(send.Span);
                    stamp = sw.ElapsedMilliseconds + 1000;
                }
                else
                {
                    UnsafeByteBuffer.Return(send);
                    UnsafeByteBuffer.Return(recv);
                    return 2;
                }
            }
            if (!socket.Poll(100000, SelectMode.SelectRead))
            {
                continue;
            }
            if (Volatile.Read(ref version) != v)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                return 1;
            }
            int count = socket.Receive(recv.FullSpan);
            if (count <= 0)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                return 2;
            }
            if (count < 10)
            {
                continue;
            }
            recv.SetWPos(10);
            recv.SetRPos(0);
            var id = recv.ReadUShort();
            if (id != NetMsgId.Message_SCSession)
            {
                continue;
            }
            var code = recv.ReadInt();
            if (code == 1)
            {
                conv = recv.ReadUInt();
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                return 0;
            }
            else
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                return 3;
            }
        }
    }
    public void Close()
    {
        Interlocked.Increment(ref version);
        Volatile.Write(ref state, (int)ConnectState.Idle);
        cts?.Cancel();
        cts = null;
        socket?.Dispose();
        socket = null;
        signal?.Release();
        signal = null;
        sendQueue1?.Clear();
        sendQueue1 = null;
        if (sendQueue2 != null)
        {
            while (sendQueue2.TryDequeue(out var packet)) packet.Dispose();
            sendQueue2 = null;
        }
        kcp?.Dispose();
        kcp = null;
    }
    public void Dispose()
    {
        Close();
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
    public void Send(ushort id, ISerialize msg)
    {
        if (State == ConnectState.Connected)
        {
            sendQueue1?.Enqueue(new SendItem(id, msg));
            signal?.Release();
        }
    }
    private async Task SendTask(int v, CancellationToken token)
    {
        var buffer = UnsafeByteBuffer.Rent(2048);
        int fail = 0;
        while (true)
        {
            try
            {
                await signal.WaitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                UnsafeByteBuffer.Return(buffer);
                return;
            }
            if (Volatile.Read(ref version) != v)
            {
                UnsafeByteBuffer.Return(buffer);
                return;
            }
            while (sendQueue1.TryDequeue(out var item))
            {
                item.Serialize(buffer);
                kcp.Send(buffer.Span);
            }

            while (sendQueue2.TryDequeue(out var item))
            {
                int count = await socket.SendAsync2(item.Datas, token).ConfigureAwait(false);
                int length = item.Length;
                item.Dispose();
                if (Volatile.Read(ref version) != v)
                {
                    UnsafeByteBuffer.Return(buffer);
                    return;
                }
                if (count == length)
                {
                    fail = 0;
                }
                else if (fail++ > 3)
                {
                    //单个发送失败KCP重传，连续失败多次重连
                    UnsafeByteBuffer.Return(buffer);
                    Connect();
                    return;
                }
            }
        }
    }
    private async Task UpdateTask(int v, CancellationToken token)
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
            if (Volatile.Read(ref version) != v)
            {
                return;
            }
        }
    }
    /// <summary>
    /// kcp.Update 中执行
    /// </summary>
    private void Output(IMemoryOwner<byte> owner, int length)
    {
        if (State == ConnectState.Connected)
        {
            sendQueue2.Enqueue(new KcpPacket(owner, length));
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
    private async Task RecvTask(int v, CancellationToken token)
    {
        var buffer = UnsafeByteBuffer.Rent(2048);
        while (true)
        {
            int count = await socket.ReceiveAsync(buffer.Memory, token).ConfigureAwait(false);
            if (Volatile.Read(ref version) != v)
            {
                UnsafeByteBuffer.Return(buffer);
                return;
            }
            if (count < 0 || !Deserialize(buffer, count))
            {
                UnsafeByteBuffer.Return(buffer);
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
                if (!dispatch.Deserialize(buffer)) return false;
            }
        }
        catch
        {
            return false;
        }
    }
    #endregion
}
