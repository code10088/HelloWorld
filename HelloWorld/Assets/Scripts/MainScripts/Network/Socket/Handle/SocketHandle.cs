using System;
using System.Net;
using System.Net.Sockets;

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
    /// <summary>
    /// TCP流式发送
    /// </summary>
    public int Send(ReadOnlySpan<byte> buffer, int length)
    {
        int count = 0;
        while (count < length)
        {
            int l = 0;
            try
            {
                l = socket.Send(buffer.Slice(count, length - count), SocketFlags.None);
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
    public void Dispose()
    {
        socket?.Close();
        socket?.Dispose();
        socket = null;
    }
}
