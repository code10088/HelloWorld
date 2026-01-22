using System;
using System.Buffers;
using System.Net.Sockets.Kcp;

public class KcpHandle
{
    private uint connectId;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private DateTimeOffset next;
    private Func<byte[], int, bool> deserialize;

    public KcpHandle(uint connectId, Action<IMemoryOwner<byte>, int> send, Func<byte[], int, bool> deserialize)
    {
        this.connectId = connectId;
        kcpSend = new KcpSend(send);
        this.deserialize = deserialize;
    }
    public void Start()
    {
        kcp = new PoolSegManager.Kcp(connectId, kcpSend);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();
        next = DateTime.UtcNow;
    }
    public void Send(ReadOnlySpan<byte> data)
    {
        kcp.Send(data);
    }
    public double Update()
    {
        var current = DateTime.UtcNow;
        if (current >= next)
        {
            kcp.Update(current);
            next = kcp.Check(current);
        }
        return (next - current).TotalMilliseconds;
    }
    public bool Deserialize(byte[] buffer, int length, ref int retry)
    {
        kcp.Input(buffer.AsSpan(0, length));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) break;
            var temp = BufferPool.Rent(size);
            size = kcp.Recv(temp);
            if (size <= 0)
            {
                temp.Return();
                break;
            }
            else
            {
                bool b = deserialize(temp, size);
                temp.Return();
                if (b) retry = 0;
                else return false;
            }
        }
        return true;
    }
    public void Dispose()
    {
        kcp?.Dispose();
        kcp = null;
    }
}
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
public struct KcpPacket
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