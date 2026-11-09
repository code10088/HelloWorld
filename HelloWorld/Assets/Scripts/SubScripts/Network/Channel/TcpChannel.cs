using System;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TcpChannel : ChannelInterface
{
    private string ip;
    private ushort port;
    private IDispatch dispatch;
    private TcpContent content;

    public ConnectState State => content?.State ?? ConnectState.Idle;

    public void Init(string ip, ushort port, IDispatch dispatch)
    {
        this.ip = ip;
        this.port = port;
        this.dispatch = dispatch;
    }
    public void Connect()
    {
        content?.Dispose();
        content = new TcpContent(this);
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


    private class TcpContent
    {
        private TcpChannel channel;
        private SocketHandle socket;
        private SemaphoreSlim signal = new SemaphoreSlim(0);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ConcurrentQueue<SendItem> sendQueue = new ConcurrentQueue<SendItem>();
        private int state = (int)ConnectState.Idle;

        public ConnectState State => (ConnectState)Volatile.Read(ref state);

        public TcpContent(TcpChannel channel)
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
                channel.dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
                return;
            }
            var socket = new SocketHandle(channel.ip, channel.port);
            var success = socket.Connect(SocketType.Stream, ProtocolType.Tcp);
            if (State == ConnectState.Close)
            {
                socket.Dispose();
                return;
            }
            if (!success)
            {
                socket.Dispose();
                channel.dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
                return;
            }
            this.socket = socket;
            _ = SendTask(cts.Token);
            _ = RecvTask(cts.Token);
            Volatile.Write(ref state, (int)ConnectState.Connected);
            channel.dispatch.HandleNetEvent(NetEvent.Connected, 0);
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
            sendQueue?.Clear();
            sendQueue = null;
        }
        #endregion

        #region 发送
        public void Send(ushort id, ISerialize msg)
        {
            sendQueue?.Enqueue(new SendItem(id, msg));
            signal?.Release();
        }
        private async Task SendTask(CancellationToken token)
        {
            var buffer = UnsafeByteBuffer.Rent(10240);
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
                while (sendQueue.TryDequeue(out var item))
                {
                    item.Serialize(buffer, true);
                    int length = buffer.WPos;
                    int count = await socket.SendAsync1(buffer.Mem, token).ConfigureAwait(false);
                    if (State == ConnectState.Close)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        return;
                    }
                    if (count != length)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        channel.Connect();
                        return;
                    }
                }
            }
        }
        #endregion

        #region 接收
        private async Task RecvTask(CancellationToken token)
        {
            var recv = UnsafeByteBuffer.Rent(10240);
            var head = UnsafeByteBuffer.Rent(4);
            var body = UnsafeByteBuffer.Rent(10240);
            int l = 0;
            while (true)
            {
                int count = await socket.ReceiveAsync(recv.Memory, token).ConfigureAwait(false);
                if (State == ConnectState.Close)
                {
                    UnsafeByteBuffer.Return(recv);
                    UnsafeByteBuffer.Return(head);
                    UnsafeByteBuffer.Return(body);
                    return;
                }
                if (count <= 0 || !Deserialize(recv, head, body, count, ref l))
                {
                    UnsafeByteBuffer.Return(recv);
                    UnsafeByteBuffer.Return(head);
                    UnsafeByteBuffer.Return(body);
                    channel.Connect();
                    return;
                }
            }
        }
        private bool Deserialize(UnsafeByteBuffer recv, UnsafeByteBuffer head, UnsafeByteBuffer body, int length, ref int count)
        {
            int pos = 0;
            while (pos < length)
            {
                if (head.WPos < 4)
                {
                    int l = Math.Min(4 - head.WPos, length - pos);
                    head.WriteBuffer(recv, pos, l);
                    pos += l;
                    if (head.WPos == 4)
                    {
                        head.SetRPos(0);
                        count = head.ReadInt();
                        if (count < 2 || count > body.Capacity)
                        {
                            head.Clear();
                            body.Clear();
                            count = 0;
                            return false;
                        }
                    }
                }
                else
                {
                    int l = Math.Min(count - body.WPos, length - pos);
                    body.WriteBuffer(recv, pos, l);
                    pos += l;
                    if (body.WPos == count)
                    {
                        var b = channel.dispatch.Deserialize(body);
                        head.Clear();
                        body.Clear();
                        count = 0;
                        if (!b) return false;
                    }
                }
            }
            return true;
        }
        #endregion
    }
}
