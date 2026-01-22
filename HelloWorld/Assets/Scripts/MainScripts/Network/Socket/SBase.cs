using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
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
    private IExtensible msg;
    public ushort Id => id;
    public IExtensible Msg => msg;
    public SendItem(ushort id, IExtensible msg)
    {
        this.id = id;
        this.msg = msg;
    }
}
public class SBase
{
    private Func<ushort, Memory<byte>, bool> deserialize;
    protected Action<int, int> socketevent;
    protected SocketHandle socket;
    protected SerializeHandle serialize;
    private HeartHandle heart;
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
    protected byte[] receiveBuffer = new byte[2048];
    protected int receiveRetry = 0;

    public virtual void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
        socket = new SocketHandle(ip, port);
        serialize = new SerializeHandle(Receive);
        heart = new HeartHandle(Connect, Send);
        Connect();
    }

    #region 连接
    public void Reconnect()
    {
        connectMark = false;
        connectRetry = 0;
        Connect();
    }
    protected virtual async Task<bool> Connect()
    {
        await Close();
        if (connectRetry++ > 3)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return false;
        }
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return false;
        }
        heart.Start();
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
    public virtual void Send(ushort id, IExtensible msg)
    {
        if (connectMark)
        {
            heart.RefreshDelay1(id);
            sendQueue.Enqueue(new SendItem(id, msg));
        }
    }
    #endregion

    #region 接收
    protected bool Receive(byte[] bytes, int length)
    {
        var temp = bytes.AsMemory(0, length);
        var id = BitConverter.ToUInt16(temp.Span.Slice(0, 2));
        var b = deserialize(id, temp.Slice(2));
        heart.RefreshDelay2(id);
        socketevent.Invoke((int)SocketEvent.RefreshDelay, heart.Delay);
        return b;
    }
    #endregion
}