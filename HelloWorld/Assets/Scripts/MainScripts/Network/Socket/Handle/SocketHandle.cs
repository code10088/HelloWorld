using System;
using System.Net;
using System.Net.Sockets;

public class SocketHandle
{
    private EndPoint endPoint;
    private Socket socket;
    private int timeout = 10000;

    public bool Connected => socket.Connected;

    public SocketHandle(string ip, ushort port)
    {
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
    }
    public void Connect(SocketType st, ProtocolType pt)
    {
        socket = new Socket(AddressFamily.InterNetwork, st, pt);
        socket.SendTimeout = timeout;
        socket.ReceiveTimeout = timeout;
        try
        {
            socket.Connect(endPoint);
        }
        catch
        {

        }
    }
    /// <summary>
    /// TCP流式发送
    /// </summary>
    public int Send(byte[] buffer, int length)
    {
        int count = 0;
        while (count < length)
        {
            int l = 0;
            try
            {
                l = socket.Send(buffer, count, length - count, SocketFlags.None);
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
    public int Receive(byte[] buffer)
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
