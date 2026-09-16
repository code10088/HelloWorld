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
    public void Serialize(UnsafeByteBuffer buffer, bool writelength = false)
    {
        buffer.SetWPos(0);
        if (writelength) buffer.WriteInt(0);
        buffer.WriteUShort(id);
        msg.Serialize(buffer);
        if (writelength) buffer.WriteValueAt(0, buffer.WPos - 4);
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
    public bool Connected
    {
        get => Interlocked.CompareExchange(ref connectFlag, 0, 0) == 1;
        set => Interlocked.Exchange(ref connectFlag, value ? 1 : 0);
    }
    protected int connectRetry = 0;
    //发送
    protected ConcurrentQueue<SendItem> sendQueue = new ConcurrentQueue<SendItem>();
    protected UnsafeByteBuffer sendBuffer;
    //接收
    protected UnsafeByteBuffer receiveBuffer;

    public virtual void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
        socket = new SocketHandle(ip, port);
        heart = new HeartHandle(Connect, Send);
        sendBuffer = UnsafeByteBuffer.Rent(2048);
        receiveBuffer = UnsafeByteBuffer.Rent(2048);
        Connect();
    }

    #region 连接
    public void Reconnect()
    {
        Connected = false;
        connectRetry = 0;
        Connect();
    }
    protected virtual void Connect()
    {

    }
    public virtual async Task Close()
    {
        Connected = false;
        socket?.Dispose();
        await heart?.Dispose();
        sendQueue.Clear();
    }
    public virtual async Task Dispose()
    {
        await Close();
        UnsafeByteBuffer.Return(sendBuffer);
        sendBuffer = null;
        UnsafeByteBuffer.Return(receiveBuffer);
        receiveBuffer = null;
    }
    #endregion

    #region 发送
    public virtual void Send(ushort id, ISerialize msg)
    {
        if (Connected)
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
        if (id == NetMsgId.SCHeart) socketevent.Invoke((int)SocketEvent.RefreshDelay, heart.Delay);
        return b;
    }
    #endregion
}
