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
    private int version;
    private SocketHandle socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private ConcurrentQueue<SendItem> sendQueue;
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
        if (!socket.Connect(SocketType.Stream, ProtocolType.Tcp))
        {
            socket?.Dispose();
            dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
            return;
        }
        if (Volatile.Read(ref version) != v)
        {
            socket?.Dispose();
            return;
        }
        this.socket = socket;
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        sendQueue = new ConcurrentQueue<SendItem>();
        _ = SendTask(v, cts.Token);
        _ = RecvTask(v, cts.Token);
        Volatile.Write(ref state, (int)ConnectState.Connected);
        dispatch.HandleNetEvent(NetEvent.Connected, 0);
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
        sendQueue?.Clear();
        sendQueue = null;
    }
    public void Dispose()
    {
        Close();
    }
    #endregion

    #region 发送
    public void Send(ushort id, ISerialize msg)
    {
        if (State == ConnectState.Connected)
        {
            sendQueue?.Enqueue(new SendItem(id, msg));
            signal?.Release();
        }
    }
    private async Task SendTask(int v, CancellationToken token)
    {
        var buffer = UnsafeByteBuffer.Rent(2048);
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
            while (sendQueue.TryDequeue(out var item))
            {
                item.Serialize(buffer, true);
                int length = buffer.WPos;
                int count = await socket.SendAsync1(buffer.Mem, token).ConfigureAwait(false);
                if (Volatile.Read(ref version) != v)
                {
                    UnsafeByteBuffer.Return(buffer);
                    return;
                }
                if (count != length)
                {
                    UnsafeByteBuffer.Return(buffer);
                    Connect();
                    return;
                }
            }
        }
    }
    #endregion

    #region 接收
    private async Task RecvTask(int v, CancellationToken token)
    {
        var recv = UnsafeByteBuffer.Rent(2048);
        var head = UnsafeByteBuffer.Rent(4);
        var body = UnsafeByteBuffer.Rent(2048);
        int l = 0;
        while (true)
        {
            int count = await socket.ReceiveAsync(recv.Memory, token).ConfigureAwait(false);
            if (Volatile.Read(ref version) != v)
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
                Connect();
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
                    var b = dispatch.Deserialize(body);
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
