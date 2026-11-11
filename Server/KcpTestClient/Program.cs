// 最小 KCP 测试客户端，协议与 Assets/Scripts/SubScripts/Network/Channel/KcpChannel.cs 一致。
// 用法: KcpTestClient <host> <port> [token]
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Text;

internal static class Program
{
    private const ushort Message_CSSession = 0;
    private const ushort Message_SCSession = 10000;
    private const ushort Message_CSAuth = 1;
    private const ushort Message_SCAuth = 10001;
    private const ushort Message_SCPlayerData = 10003;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string host = args.Length > 0 ? args[0] : "127.0.0.1";
        int port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 8088;
        string token = args.Length > 2 ? args[2] : "test-access-token";

        using var udp = new UdpClient();
        udp.Connect(host, port);
        udp.Client.ReceiveTimeout = 1000;

        uint conv = Handshake(udp, token, 1);
        if (conv == 0)
        {
            Console.WriteLine("握手失败");
            return 1;
        }

        Console.WriteLine($"握手成功 conv={conv}");

        var callback = new OutputCallback(udp);
        var kcp = new PoolSegManager.Kcp(conv, callback);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();

        SendAuth(kcp, token);

        bool gotAuth = false;
        bool gotPlayer = false;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var remote = new IPEndPoint(IPAddress.Any, 0);

        while (DateTime.UtcNow < deadline && !(gotAuth && gotPlayer))
        {
            kcp.Update(DateTimeOffset.UtcNow);

            if (udp.Client.Poll(5000, SelectMode.SelectRead))
            {
                byte[] datagram;
                try
                {
                    datagram = udp.Receive(ref remote);
                }
                catch (SocketException)
                {
                    continue;
                }

                kcp.Input(datagram);

                while (true)
                {
                    var (buffer, length) = kcp.TryRecv();
                    if (buffer == null || length <= 0) break;

                    try
                    {
                        byte[] message = buffer.Memory.Slice(0, length).ToArray();
                        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan(0, 2));
                        Console.WriteLine($"收到 KCP 消息 id={id}, length={length}");

                        if (id == Message_SCAuth)
                        {
                            int code = BinaryPrimitives.ReadInt32LittleEndian(message.AsSpan(2, 4));
                            Console.WriteLine($"SCAuth code={code}");
                            gotAuth = code == 1;
                        }
                        else if (id == Message_SCPlayerData)
                        {
                            Console.WriteLine("SCPlayerData 已收到");
                            gotPlayer = true;
                        }
                    }
                    finally
                    {
                        buffer.Dispose();
                    }
                }
            }

            Thread.Sleep(5);
        }

        if (gotAuth && gotPlayer)
        {
            Console.WriteLine("KCP TEST OK");
            return 0;
        }

        Console.WriteLine($"KCP TEST FAILED (auth={gotAuth}, player={gotPlayer})");
        return 1;
    }

    private static uint Handshake(UdpClient udp, string token, int version)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        var packet = new List<byte>();
        WriteUShort(packet, Message_CSSession);
        WriteUShort(packet, (ushort)tokenBytes.Length);
        packet.AddRange(tokenBytes);
        WriteInt(packet, version);

        var remote = new IPEndPoint(IPAddress.Any, 0);

        for (int retry = 0; retry < 6; retry++)
        {
            udp.Send(packet.ToArray(), packet.Count);
            try
            {
                byte[] response = udp.Receive(ref remote);
                if (response.Length < 10) continue;

                ushort id = BinaryPrimitives.ReadUInt16LittleEndian(response.AsSpan(0, 2));
                int code = BinaryPrimitives.ReadInt32LittleEndian(response.AsSpan(2, 4));
                uint conv = BinaryPrimitives.ReadUInt32LittleEndian(response.AsSpan(6, 4));

                Console.WriteLine($"握手响应 id={id}, code={code}, conv={conv}");
                if (id == Message_SCSession && code == 1) return conv;
            }
            catch (SocketException)
            {
                // timeout, retry
            }
        }

        return 0;
    }

    private static void SendAuth(PoolSegManager.Kcp kcp, string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        var body = new List<byte>();
        WriteUShort(body, Message_CSAuth);
        WriteUShort(body, (ushort)tokenBytes.Length);
        body.AddRange(tokenBytes);

        int result = kcp.Send(body.ToArray());
        Console.WriteLine($"发送 CSAuth，结果={result}");
    }

    private static void WriteUShort(List<byte> b, ushort v)
    {
        Span<byte> tmp = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(tmp, v);
        b.Add(tmp[0]);
        b.Add(tmp[1]);
    }

    private static void WriteInt(List<byte> b, int v)
    {
        Span<byte> tmp = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(tmp, v);
        b.Add(tmp[0]); b.Add(tmp[1]); b.Add(tmp[2]); b.Add(tmp[3]);
    }

    private sealed class OutputCallback : IKcpCallback
    {
        private readonly UdpClient udp;

        public OutputCallback(UdpClient udp)
        {
            this.udp = udp;
        }

        public void Output(IMemoryOwner<byte> owner, int avalidLength)
        {
            try
            {
                if (avalidLength > 0)
                {
                    byte[] data = owner.Memory.Slice(0, avalidLength).ToArray();
                    udp.Send(data, data.Length);
                }
            }
            finally
            {
                owner.Dispose();
            }
        }
    }
}