using ProtoBuf;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public enum SocketEvent
{
    Reconect,
    Connected,
    ConnectError,
    RefreshDelay,
}
public class SBase
{
    public class SendItem
    {
        public ushort id;
        public IExtensible msg;
    }
    [ProtoContract]
    public class CS_Heart : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
    }

    
    private Func<ushort, Memory<byte>, bool> deserialize;
    protected Action<int, int> socketevent;
    protected ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;

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

    //心跳
    private bool heartMark = true;
    private int heartTimerId = 0;
    private float heartTimer = 0;
    protected int heartInterval = 10000;
    private int[] record1 = new int[10];
    private long[] record2 = new long[10];
    private int recordIndex = 0;
    private CS_Heart heart = new CS_Heart();

    public virtual void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
    }

    #region 连接
    public virtual async Task Close()
    {
        connectMark = false;
        connectRetry = 0;
        sendQueue.Clear();
        sendRetry = 0;
        receiveRetry = 0;
        heartTimer = 0;
        heartInterval = 10000;
        recordIndex = 0;
    }
    #endregion

    #region 发送
    public virtual void Send(ushort id, IExtensible msg)
    {
        RefreshDelay1(id);
        sendQueue.Enqueue(new SendItem { id = id, msg = msg });
    }
    #endregion

    #region 接收
    protected bool Deserialize(byte[] bytes, int length)
    {
        var temp = bytes.AsMemory(0, length);
        var id = BitConverter.ToUInt16(temp.Span.Slice(0, 2));
        var b = deserialize(id, temp.Slice(2));
        RefreshDelay2(id);
        return b;
    }
    #endregion

    #region 心跳
    public void SetHeartState(bool open)
    {
        heartMark = open;
        if (open) heartTimerId = Driver.Instance.StartTimer(0, 1, UpdateHeart);
        else Driver.Instance.Remove(heartTimerId);
    }
    private void UpdateHeart(float t)
    {
        if (!heartMark || !connectMark) return;
        heartTimer++;
        if (heartTimer < heartInterval) return;
        Send(0, heart);
        heartTimer = 0;
    }
    private void RefreshDelay1(ushort id)
    {
        int index = recordIndex++ % record1.Length;
        record1[index] = id;
        record2[index] = DateTime.UtcNow.Ticks;
    }
    private void RefreshDelay2(ushort id)
    {
        var index = Array.IndexOf(record1, id - 10000);
        if (index < 0) return;
        record1[index] = -1;
        var delay = (DateTime.UtcNow.Ticks - record2[index]) / 10000;
        heartTimer = 0;
        heartInterval = delay > 100 ? 2000 : 10000;
        socketevent.Invoke((int)SocketEvent.RefreshDelay, (int)delay);
    }
    #endregion
}
public class WriteBuffer : Stream
{
    private ArrayPool<byte> pool;
    private byte[] buffer;
    private int pos;

    public byte[] Buffer => buffer;
    public int Pos => pos;

    public WriteBuffer(ArrayPool<byte> pool, int size = 256, int offset = 0)
    {
        this.pool = pool;
        buffer = pool.Rent(size);
        pos = offset;
    }
    public override void Write(byte[] bytes, int offset, int count)
    {
        if (count == 0) return;
        if (pos + count > buffer.Length)
        {
            var _buffer = pool.Rent(Math.Max(pos + count, buffer.Length * 2));
            System.Buffer.BlockCopy(buffer, 0, _buffer, 0, pos);
            pool.Return(buffer);
            buffer = _buffer;
        }
        System.Buffer.BlockCopy(bytes, offset, buffer, pos, count);
        pos += count;
    }
    protected override void Dispose(bool disposing)
    {
        if (buffer != null)
        {
            pool.Return(buffer);
            buffer = null;
        }
        base.Dispose(disposing);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => pos;
    public override long Position { get => pos; set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int ReadByte() => throw new NotSupportedException();
    public override void Flush() { }
}