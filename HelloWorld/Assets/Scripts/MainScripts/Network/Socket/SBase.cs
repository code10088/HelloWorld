using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public enum SocketEvent
{
    Reconect,
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
    public UnsafeByteBuffer Serialize(bool writelength = false)
    {
        var buffer = UnsafeByteBuffer.Rent(256);
        if (writelength) buffer.WriteInt(0);
        buffer.WriteUShort(id);
        msg.Serialize(buffer);
        if (writelength) buffer.WriteValueAt(0, buffer.WPos - 4);
        return buffer;
    }
}
public class SBase
{
    private Func<ushort, UnsafeByteBuffer, bool> deserialize;
    protected Action<int, int> socketevent;
    protected SocketHandle socket;
    protected HeartHandle heart;
    //连接
    private int connectFlag = 0;
    protected bool connectMark
    {
        get => Interlocked.CompareExchange(ref connectFlag, 0, 0) == 1;
        set => Interlocked.Exchange(ref connectFlag, value ? 1 : 0);
    }
    protected int connectRetry = 0;
    //发送
    protected ConcurrentQueue<SendItem> sendQueue = new ConcurrentQueue<SendItem>();
    protected int sendRetry = 0;
    //接收
    protected UnsafeByteBuffer receiveBuffer;
    protected int receiveRetry = 0;

    public virtual void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
        socket = new SocketHandle(ip, port);
        heart = new HeartHandle(Connect, Send);
        receiveBuffer = UnsafeByteBuffer.Rent(2048);
        Connect();
    }

    #region 连接
    public void Reconnect()
    {
        connectMark = false;
        connectRetry = 0;
        Connect();
    }
    protected virtual void Connect()
    {

    }
    protected virtual async Task<bool> ConnectAsync()
    {
        await Close();
        if (connectRetry++ > 0)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return false;
        }
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        return true;
    }
    public virtual async Task Close()
    {
        connectMark = false;
        socket.Dispose();
        heart.Dispose();
        sendQueue.Clear();
        sendRetry = 0;
        receiveRetry = 0;
    }
    #endregion

    #region 发送
    public virtual void Send(ushort id, ISerialize msg)
    {
        if (connectMark)
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
        var b = deserialize(id, buffer);
        heart.RefreshDelay2(id);
        socketevent.Invoke((int)SocketEvent.RefreshDelay, heart.Delay);
        return b;
    }
    #endregion
}