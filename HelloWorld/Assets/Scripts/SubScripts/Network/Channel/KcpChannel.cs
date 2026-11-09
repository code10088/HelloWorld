using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

public class KcpChannel : IChannel
{
    private string ip;
    private ushort port;
    private IChannelHandle handle;
    private KcpContent content;
    private int version;

    public ConnectState State => content?.State ?? ConnectState.Idle;

    public void Init(string ip, ushort port, IChannelHandle handle)
    {
        this.ip = ip;
        this.port = port;
        this.handle = handle;
    }
    public void Connect()
    {
        version++;
        content?.Dispose();
        content = new KcpContent(this);
        content.Connect();
    }
    public void Close()
    {
        content?.Dispose();
    }
    public void Dispose()
    {
        content?.Dispose();
    }
    public void Send(ushort id, ISerialize msg)
    {
        content?.Send(id, msg);
    }


    private class KcpContent
    {
        private KcpChannel channel;
        private SocketHandle socket;
        private SemaphoreSlim signal = new SemaphoreSlim(0);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ConcurrentQueue<SendItem> sendQueue1 = new ConcurrentQueue<SendItem>();
        private ConcurrentQueue<KcpPacket> sendQueue2 = new ConcurrentQueue<KcpPacket>();
        private KcpSend kcpSend;
        private PoolSegManager.Kcp kcp;
        private DateTimeOffset next;
        private int state = (int)ConnectState.Idle;

        public ConnectState State => (ConnectState)Volatile.Read(ref state);

        public KcpContent(KcpChannel channel)
        {
            this.channel = channel;
        }

        #region 连接
        public void Connect()
        {
            Task.Run(ConnectTask);
        }
        private async Task ConnectTask()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                channel.handle.HandleEvent(NetEvent.ConnectError, 0);
                return;
            }
            var socket = new SocketHandle(channel.ip, channel.port);
            var success = socket.Connect(SocketType.Dgram, ProtocolType.Udp);
            if (State == ConnectState.Close)
            {
                socket.Dispose();
                return;
            }
            if (!success)
            {
                socket.Dispose();
                channel.handle.HandleEvent(NetEvent.ConnectError, 0);
                return;
            }
            var send = UnsafeByteBuffer.Rent(10240);
            var recv = UnsafeByteBuffer.Rent(10240);
            success = Handshake(socket, send, recv, out uint conv);
            if (State == ConnectState.Close)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                socket.Dispose();
                return;
            }
            if (!success)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                socket.Dispose();
                channel.handle.HandleEvent(NetEvent.ConnectError, 0);
                return;
            }
            if (conv == 0)
            {
                UnsafeByteBuffer.Return(send);
                UnsafeByteBuffer.Return(recv);
                socket.Dispose();
                channel.handle.HandleEvent(NetEvent.InvalidToken, 0);
                return;
            }
            this.socket = socket;
            kcpSend = new KcpSend(Output);
            kcp = new PoolSegManager.Kcp(conv, kcpSend);
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize();
            kcp.SetMtu();
            next = DateTime.UtcNow;
            _ = SendTask(send, cts.Token);
            _ = UpdateTask(cts.Token);
            _ = RecvTask(recv, cts.Token);
            Volatile.Write(ref state, (int)ConnectState.Connected);
            channel.handle.HandleEvent(NetEvent.Connected, 0);
        }
        private bool Handshake(SocketHandle socket, UnsafeByteBuffer send, UnsafeByteBuffer recv, out uint conv)
        {
            conv = 0;
            var retry = 0;
            var sw = Stopwatch.StartNew();
            var stamp = sw.ElapsedMilliseconds;
            send.SetWPos(0);
            send.WriteUShort(NetMsgId.Message_CSSession);
            send.WriteString(channel.handle.Token);
            send.WriteInt(channel.version);
            while (true)
            {
                if (State == ConnectState.Close) return false;
                if (sw.ElapsedMilliseconds >= stamp)
                {
                    if (retry++ >= 6) return false;
                    socket.Send(send.Span);
                    stamp = sw.ElapsedMilliseconds + 1000;
                }
                if (!socket.Poll(100000, SelectMode.SelectRead)) continue;
                if (State == ConnectState.Close) return false;
                int count = socket.Receive(recv.FullSpan);
                if (count <= 0) return false;
                if (count < 10) continue;
                recv.SetWPos(10);
                recv.SetRPos(0);
                if (recv.ReadUShort() != NetMsgId.Message_SCSession) continue;
                if (recv.ReadInt() == 1) conv = recv.ReadUInt();
                return true;
            }
        }
        public void Dispose()
        {
            Volatile.Write(ref state, (int)ConnectState.Close);
            cts?.Cancel();
            cts = null;
            socket?.Dispose();
            socket = null;
            signal?.Release();
            signal = null;
            sendQueue1?.Clear();
            sendQueue1 = null;
            while (sendQueue2.TryDequeue(out var packet)) packet.Dispose();
            kcp?.Dispose();
            kcp = null;
        }
        #endregion

        #region 发送
        private class KcpSend : IKcpCallback
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
            sendQueue1?.Enqueue(new SendItem(id, msg));
            signal?.Release();
        }
        private async Task SendTask(UnsafeByteBuffer buffer, CancellationToken token)
        {
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
                if (State == ConnectState.Close)
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
                    if (State == ConnectState.Close)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        return;
                    }
                    if (count == length)
                    {
                        //单次发送失败：KCP重传
                        fail = 0;
                    }
                    else if (fail++ > 3)
                    {
                        //连续发送失败：重连
                        UnsafeByteBuffer.Return(buffer);
                        channel.Connect();
                        return;
                    }
                }
            }
        }
        private async Task UpdateTask(CancellationToken token)
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
                }
                if (State == ConnectState.Close)
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
        private async Task RecvTask(UnsafeByteBuffer buffer, CancellationToken token)
        {
            while (true)
            {
                int count = await socket.ReceiveAsync(buffer.Memory, token).ConfigureAwait(false);
                if (State == ConnectState.Close)
                {
                    UnsafeByteBuffer.Return(buffer);
                    return;
                }
                if (count < 0 || !Deserialize(buffer, count))
                {
                    UnsafeByteBuffer.Return(buffer);
                    channel.Connect();
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
                    if (!channel.handle.Deserialize(buffer)) return false;
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
