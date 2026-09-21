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
public enum SocketEvent
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
    protected HeartHandle heart;
    //连接
    private int state = (int)ConnectState.Idle;
    public ConnectState State
    {
        get => (ConnectState)Volatile.Read(ref state);
        set => Volatile.Write(ref state, (int)value);
    }
    private ConcurrentQueue<ConnectState> stateQueue = new ConcurrentQueue<ConnectState>();
    private SemaphoreSlim signal = new SemaphoreSlim(0);
    private CancellationTokenSource cts = new CancellationTokenSource();
    private Task stateTask;
    protected int connectRetry = 0;
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
        stateTask = Process(cts.Token);
        Connect();
    }

    #region 连接
    protected void Connect()
    {
        SetState(ConnectState.Connect);
    }
    public void Reconnect()
    {
        connectRetry = 0;
        SetState(ConnectState.Connect);
    }
    public void Close()
    {
        SetState(ConnectState.Close);
    }
    public void Dispose()
    {
        SetState(ConnectState.Dispose);
    }
    private void SetState(ConnectState target)
    {
        if (State == ConnectState.Dispose) return;
        stateQueue.Enqueue(target);
        signal.Release();
    }
    private async Task Process(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await signal.WaitAsync(token).ConfigureAwait(false); ;
            }
            catch
            {
                return;
            }
            if (stateQueue.TryDequeue(out var target))
            {
                try
                {
                    await Process(target).ConfigureAwait(false);
                }
                catch
                {
                    State = ConnectState.Idle;
                }
            }
        }
    }
    private async Task Process(ConnectState target)
    {
        State = target;
        switch (target)
        {
            case ConnectState.Idle:
                break;
            case ConnectState.Connect:
                await CloseTask();
                await ConnectTask();
                break;
            case ConnectState.Connected:
                break;
            case ConnectState.Close:
                await CloseTask();
                State = ConnectState.Idle;
                break;
            case ConnectState.Dispose:
                await DisposeTask();
                break;
        }
    }
    protected abstract Task ConnectTask();
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
        if (id == NetMsgId.SCHeart) dispatch.HandleSocketEvent(SocketEvent.RefreshDelay, heart.Delay);
        return b;
    }
    #endregion
}
