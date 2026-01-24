using System;
using System.Buffers;
using System.Net.Sockets.Kcp;

public class KcpHandle
{
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private DateTimeOffset next;
    private CS_KcpConnect kcpConnect;
    private Func<byte[], int, bool> receive;

    public CS_KcpConnect CS_KcpConnect => kcpConnect;

    public KcpHandle(uint playerId, string token, Action<IMemoryOwner<byte>, int> send, Func<byte[], int, bool> receive)
    {
        kcpSend = new KcpSend(send);
        this.receive = receive;
        kcpConnect = new CS_KcpConnect();
        kcpConnect.playerId = playerId;
        kcpConnect.token = token;
    }
    public void Start(uint connectId)
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
    public bool Deserialize(byte[] buffer, int length)
    {
        kcp.Input(buffer.AsSpan(0, length));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) return true;
            size = kcp.Recv(buffer);
            if (size <= 0) return true;
            if (receive(buffer, size) == false) return false;
        }
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