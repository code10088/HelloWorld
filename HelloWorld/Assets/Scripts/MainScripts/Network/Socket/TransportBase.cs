using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public enum ConnectState
{
    Idle,
    Connect,
    Connected,
    Close,
    Dispose,
}
public enum NetEvent
{
    Reconnect,
    Connected,
    ConnectError,
    RefreshDelay,
}
public struct SendItem
{
    private ushort id;
    private ISerialize msg;
    public SendItem(ushort id, ISerialize msg)
    {
        this.id = id;
        this.msg = msg;
    }
    public void Serialize(UnsafeByteBuffer buffer, bool writelength = false)
    {
        buffer.SetWPos(0);
        if (writelength) buffer.WriteInt(0);
        buffer.WriteUShort(id);
        msg.Serialize(buffer);
        if (writelength) buffer.WriteValueAt(0, buffer.WPos - 4);
    }
}
public abstract class TransportBase
{
    protected IDispatch dispatch;
    private HeartHandle heart;
    //连接
    private int state = (int)ConnectState.Idle;
    public ConnectState State => (ConnectState)Volatile.Read(ref state);
    private ConcurrentQueue<ConnectState> stateQueue = new ConcurrentQueue<ConnectState>();
    private SemaphoreSlim signal = new SemaphoreSlim(0);
    private CancellationTokenSource cts = new CancellationTokenSource();
    private int retry = 0;
    //发送
    protected ConcurrentQueue<SendItem> sendQueue = new ConcurrentQueue<SendItem>();
    protected UnsafeByteBuffer sendBuffer;
    //接收
    protected UnsafeByteBuffer receiveBuffer;

    public virtual void Init(string ip, ushort port, uint playerId, string token, IDispatch dispatch)
    {
        this.dispatch = dispatch;
        heart = new HeartHandle(Connect, Send);
        sendBuffer = UnsafeByteBuffer.Rent(2048);
        receiveBuffer = UnsafeByteBuffer.Rent(2048);
        Process(cts.Token);
        Connect();
    }

    #region 连接
    protected void Connect()
    {
        if (State == ConnectState.Dispose) return;
        stateQueue.Enqueue(ConnectState.Connect);
        signal.Release();
    }
    public void Reconnect()
    {
        retry = 0;
        Connect();
    }
    public void Close()
    {
        if (State == ConnectState.Dispose) return;
        stateQueue.Enqueue(ConnectState.Close);
        signal.Release();
    }
    public void Dispose()
    {
        if (State == ConnectState.Dispose) return;
        stateQueue.Enqueue(ConnectState.Dispose);
        signal.Release();
    }
    private async Task Process(CancellationToken token)
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
            if (stateQueue.TryDequeue(out var target))
            {
                try
                {
                    Volatile.Write(ref state, (int)target);
                    switch (target)
                    {
                        case ConnectState.Idle:
                            break;
                        case ConnectState.Connect:
                            await CloseTask();
                            if (retry++ > 0)
                            {
                                dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
                                return;
                            }
                            var success = await TestTask();
                            if (!success)
                            {
                                dispatch.HandleNetEvent(NetEvent.ConnectError, 0);
                                return;
                            }
                            dispatch.HandleNetEvent(NetEvent.Reconnect, 0);
                            success = await ConnectTask();
                            if (!success)
                            {
                                Connect();
                                return;
                            }
                            Volatile.Write(ref state, (int)ConnectState.Connected);
                            retry = 0;
                            heart.Start();
                            dispatch.HandleNetEvent(NetEvent.Connected, 0);
                            break;
                        case ConnectState.Connected:
                            break;
                        case ConnectState.Close:
                            await CloseTask();
                            Volatile.Write(ref state, (int)ConnectState.Idle);
                            break;
                        case ConnectState.Dispose:
                            await DisposeTask();
                            break;
                    }
                }
                catch
                {
                    Volatile.Write(ref state, (int)ConnectState.Idle);
                }
            }
        }
    }
    protected abstract Task<bool> TestTask();
    protected abstract Task<bool> ConnectTask();
    protected virtual async Task CloseTask()
    {
        await heart.Dispose();
        sendQueue.Clear();
    }
    protected virtual async Task DisposeTask()
    {
        await CloseTask();
        cts?.Cancel();
        signal?.Release();
        cts?.Dispose();
        signal?.Dispose();
        cts = null;
        signal = null;
        stateQueue.Clear();
        UnsafeByteBuffer.Return(sendBuffer);
        sendBuffer = null;
        UnsafeByteBuffer.Return(receiveBuffer);
        receiveBuffer = null;
        dispatch = null;
    }
    #endregion

    #region 发送
    public virtual void Send(ushort id, ISerialize msg)
    {
        if (State == ConnectState.Connected)
        {
            heart.RefreshDelay1(id);
            sendQueue.Enqueue(new SendItem(id, msg));
        }
    }
    #endregion

    #region 接收
    protected bool Receive(UnsafeByteBuffer buffer)
    {
        var id = buffer.ReadUShort();
        var b = dispatch.Deserialize(id, buffer);
        heart.RefreshDelay2(id);
        if (id == NetMsgId.SCHeart) dispatch.HandleNetEvent(NetEvent.RefreshDelay, heart.Delay);
        return b;
    }
    #endregion
}
