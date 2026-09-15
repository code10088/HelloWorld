using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class SocketHandle
{
    private string host;
    private ushort port;
    private Socket socket;
    private int timeout = 10000;

    public bool Connected => socket != null && socket.Connected;

    public SocketHandle(string host, ushort port)
    {
        this.host = host;
        this.port = port;
    }
    public void Dispose()
    {
        socket?.Close();
        socket?.Dispose();
        socket = null;
    }

    #region 同步
    public bool Connect(SocketType st, ProtocolType pt)
    {
        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(host);
        }
        catch
        {
            return false;
        }
        foreach (var address in addresses)
        {
            try
            {
                socket = new Socket(address.AddressFamily, st, pt);
                socket.SendTimeout = timeout;
                socket.ReceiveTimeout = timeout;
                if (st == SocketType.Stream) socket.NoDelay = true;
                socket.Connect(new IPEndPoint(address, port));
                return true;
            }
            catch
            {
                socket?.Dispose();
            }
        }
        return false;
    }
    public int Send(ReadOnlySpan<byte> buffer)
    {
        int count = 0;
        try
        {
            count = socket.Send(buffer, SocketFlags.None);
        }
        catch
        {
            count = -1;
        }
        return count;
    }
    public int Receive(Span<byte> buffer)
    {
        int count = 0;
        try
        {
            count = socket.Receive(buffer, SocketFlags.None);
        }
        catch
        {
            count = -1;
        }
        return count;
    }
    #endregion

    #region 异步
    public async Task<int> SendAsync1(ReadOnlyMemory<byte> buffer, CancellationToken token)
    {
        int count = 0;
        while (count < buffer.Length)
        {
            int l = 0;
            try
            {
                //socket使用CancellationToken，cts?.Cancel导致await无法退出
                //l = await socket.SendAsync(buffer.Slice(count), SocketFlags.None, token).ConfigureAwait(false);
                l = await socket.SendAsync(buffer.Slice(count), SocketFlags.None).ConfigureAwait(false);
            }
            catch
            {
                l = -1;
            }
            if (l <= 0)
            {
                break;
            }
            count += l;
        }
        return count;
    }
    public async Task<int> SendAsync2(ReadOnlyMemory<byte> buffer, CancellationToken token)
    {
        int count = 0;
        try
        {
            //socket使用CancellationToken，cts?.Cancel导致await无法退出
            //count = await socket.SendAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);
            count = await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);
        }
        catch
        {
            count = -1;
        }
        return count;
    }
    public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token)
    {
        int count = 0;
        try
        {
            //socket使用CancellationToken，cts?.Cancel导致await无法退出
            //count = await socket.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);
            count = await socket.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);
        }
        catch
        {
            count = -1;
        }
        return count;
    }
    #endregion
}
