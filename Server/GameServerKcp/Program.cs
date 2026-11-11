// 测试用 KCP 游戏服务器（AIServerTest.txt 的 KCP 段）
// 握手：客户端 -> [ushort 0][ushort tokenLength][token][int32 version]
//       服务器 -> [ushort 10000][int32 1][uint32 conv]
// 消息：KCP 自带边界，帧内容为 [ushort id][payload]
//
// 注意：这是单客户端测试服务器；多客户端需要按 RemoteEndPoint 维护多个 Kcp 实例。
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Text;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        int port = int.TryParse(Environment.GetEnvironmentVariable("KCP_PORT"), out var p) ? p : 8088;
        using var server = new KcpServer(port);

        Console.WriteLine($"KCP 服务器已启动，UDP 0.0.0.0:{port}，单客户端模式");

        var quit = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            quit.Set();
        };
        AppDomain.CurrentDomain.ProcessExit += (_, __) => quit.Set();
        quit.Wait();
    }
}

internal sealed class KcpServer : IKcpCallback, IDisposable
{
    private const ushort Message_CSSession = 0;
    private const ushort Message_SCSession = 10000;
    private const ushort Message_CSAuth = 1;
    private const ushort Message_CSHeart = 2;
    private const ushort Message_CSMail = 100;
    private const ushort Message_CSReadMail = 101;
    private const ushort Message_CSGetMailReward = 102;
    private const ushort Message_CSGetMailAllReward = 103;
    private const ushort Message_CSDeleteMail = 104;
    private const ushort Message_CSDeleteAllMail = 105;
    private const ushort Message_SCAuth = 10001;
    private const ushort Message_SCHeart = 10002;
    private const ushort Message_SCPlayerData = 10003;
    private const ushort Message_SCMail = 10100;
    private const ushort Message_SCGetMailReward = 10102;
    private const ushort Message_SCGetMailAllReward = 10103;
    private const ushort Message_SCDeleteMail = 10104;

    private readonly UdpClient udp;
    private readonly object gate = new object();
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly List<TestMail> mails = CreateMails();

    private IPEndPoint remote;
    private PoolSegManager.Kcp kcp;
    private uint conv;
    private int convSeed;
    private int lastVersion;

    public KcpServer(int port)
    {
        udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        _ = Task.Run(() => ReceiveLoopAsync(cts.Token));
        _ = Task.Run(() => UpdateLoopAsync(cts.Token));
    }

    public void Output(IMemoryOwner<byte> owner, int avalidLength)
    {
        try
        {
            if (remote != null && avalidLength > 0)
            {
                byte[] data = owner.Memory.Slice(0, avalidLength).ToArray();
                udp.Send(data, data.Length, remote); // 同步发送，保证 KCP 输出顺序
            }
        }
        catch (Exception e)
        {
            Log($"UDP 发送失败: {e.Message}");
        }
        finally
        {
            owner.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await udp.ReceiveAsync();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                if (token.IsCancellationRequested) return;
                Log($"UDP 接收异常: {e.Message}");
                continue;
            }

            byte[] data = result.Buffer;

            if (IsHandshake(data))
            {
                HandleHandshake(result.RemoteEndPoint, data);
                continue;
            }

            lock (gate)
            {
                if (kcp == null) continue;

                try
                {
                    kcp.Input(data);

                    while (true)
                    {
                        var (buffer, length) = kcp.TryRecv();
                        if (buffer == null || length <= 0) break;

                        try
                        {
                            byte[] message = buffer.Memory.Slice(0, length).ToArray();
                            HandleMessage(message);
                        }
                        finally
                        {
                            buffer.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log($"KCP 接收异常: {e.Message}");
                }
            }
        }
    }

    private async Task UpdateLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                lock (gate)
                {
                    kcp?.Update(DateTimeOffset.UtcNow);
                }
            }
            catch (Exception e)
            {
                Log($"KCP Update 异常: {e.Message}");
            }

            try
            {
                await Task.Delay(10, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static bool IsHandshake(byte[] data)
    {
        if (data.Length < 8) return false;
        if (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0, 2)) != Message_CSSession)
            return false;

        int tokenLength = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(2, 2));
        return data.Length == 4 + tokenLength + 4;
    }

    private void HandleHandshake(IPEndPoint endpoint, byte[] data)
    {
        int tokenLength = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(2, 2));
        string token = Encoding.UTF8.GetString(data, 4, tokenLength);
        int version = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4 + tokenLength, 4));

        Log($"KCP 握手: token={token}, version={version}, endpoint={endpoint}");

        lock (gate)
        {
            // 客户端等待握手响应期间会每秒重发，version 不变。
            // 同版本重发只回同一个 conv，不重建 KCP，避免把已经建立的会话重置。
            if (kcp != null && remote != null && remote.Equals(endpoint) && version == lastVersion)
            {
                SendHandshakeResponse(endpoint, conv);
                return;
            }

            remote = endpoint;
            lastVersion = version;
            conv = NextConv();

            kcp?.Dispose();
            kcp = new PoolSegManager.Kcp(conv, this);
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize();
            kcp.SetMtu();

            SendHandshakeResponse(endpoint, conv);
        }
    }

    private void SendHandshakeResponse(IPEndPoint endpoint, uint sessionConv)
    {
        // 必须 10 字节：[ushort 10000][int32 1][uint32 conv]
        byte[] response = new byte[10];
        BinaryPrimitives.WriteUInt16LittleEndian(response.AsSpan(0, 2), Message_SCSession);
        BinaryPrimitives.WriteInt32LittleEndian(response.AsSpan(2, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(response.AsSpan(6, 4), sessionConv);
        udp.Send(response, response.Length, endpoint);
    }

    private uint NextConv()
    {
        uint value = unchecked((uint)Interlocked.Increment(ref convSeed));
        if (value == 0)
            value = unchecked((uint)Interlocked.Increment(ref convSeed));
        return value;
    }

    private void HandleMessage(byte[] message)
    {
        if (message.Length < 2) return;

        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan(0, 2));
        Log($"收到 KCP 消息 id={id}, length={message.Length}");

        switch (id)
        {
            case Message_CSAuth:
                {
                    string token = ReadString(message, 2);
                    Log($"鉴权 token={token}");

                    Send(Message_SCAuth, b =>
                    {
                        WriteInt(b, 1);                  // code: 1=成功
                        WriteLong(b, NowMs());           // 服务器时间
                    });

                    SendPlayerData();
                }
                break;

            case Message_CSHeart:
                Send(Message_SCHeart, b => WriteLong(b, NowMs()));
                break;

            case Message_CSMail:
                Send(Message_SCMail, b => WriteMailList(b, mails));
                break;

            case Message_CSReadMail:
                {
                    int count = message.Length >= 6
                        ? BinaryPrimitives.ReadInt32LittleEndian(message.AsSpan(2, 4))
                        : 0;
                    for (int i = 0; i < count; i++)
                    {
                        int offset = 6 + i * 4;
                        if (offset + 4 > message.Length) break;

                        uint mailId = BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(offset, 4));
                        var mail = mails.Find(m => m.id == mailId);
                        if (mail != null && mail.status == 0)
                            mail.status = 1;
                    }

                    Send(Message_SCMail, b => WriteMailList(b, mails));
                }
                break;

            case Message_CSGetMailReward:
                {
                    uint mailId = message.Length >= 6
                        ? BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(2, 4))
                        : 0;
                    var mail = mails.Find(m => m.id == mailId);
                    if (mail != null)
                        mail.status = 2;

                    Send(Message_SCGetMailReward, b =>
                    {
                        WriteInt(b, 0);                  // 0=成功
                        WriteUInt(b, mailId);
                    });
                }
                break;

            case Message_CSGetMailAllReward:
                {
                    foreach (var mail in mails)
                    {
                        if (mail.status < 2)
                            mail.status = 2;
                    }

                    Send(Message_SCGetMailAllReward, null);
                }
                break;

            case Message_CSDeleteMail:
                {
                    uint mailId = message.Length >= 6
                        ? BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(2, 4))
                        : 0;
                    mails.RemoveAll(m => m.id == mailId);

                    Send(Message_SCDeleteMail, b =>
                    {
                        WriteInt(b, 0);                  // 0=成功
                        WriteUInt(b, mailId);
                    });
                }
                break;

            case Message_CSDeleteAllMail:
                mails.Clear();
                Send(Message_SCMail, b => WriteInt(b, 0));
                break;

            default:
                Log($"未处理的 KCP 消息 id={id}");
                break;
        }
    }

    private void SendPlayerData()
    {
        Send(Message_SCPlayerData, b =>
        {
            WriteUInt(b, 10001);                       // playerId
            WriteString(b, "TestPlayer");              // nickName
            WriteString(b, "avatar_01");               // avatar
            WriteUInt(b, 1);                           // level
            WriteLong(b, DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // registerTime
            WriteUInt(b, 0);                           // mail
        });
    }

    private void Send(ushort id, Action<List<byte>> writePayload)
    {
        if (kcp == null)
        {
            Log($"KCP 尚未建立，丢弃消息 id={id}");
            return;
        }

        var body = new List<byte>();
        WriteUShort(body, id);
        writePayload?.Invoke(body);

        int result = kcp.Send(body.ToArray());
        if (result < 0)
            Log($"KCP Send 失败: {result}, id={id}");
    }

    public void Dispose()
    {
        cts.Cancel();
        udp.Dispose();
        lock (gate)
        {
            kcp?.Dispose();
            kcp = null;
        }
        cts.Dispose();
    }

    private sealed class TestMail
    {
        public uint id;
        public uint status;
        public string title;
        public string content;
        public List<TestReward> rewards = new List<TestReward>();
    }

    private sealed class TestReward
    {
        public uint itemId;
        public uint count;
    }

    private static List<TestMail> CreateMails()
    {
        return new List<TestMail>
        {
            new TestMail
            {
                id = 1,
                status = 0,
                title = "测试邮件1",
                content = "欢迎来到游戏",
                rewards = new List<TestReward>
                {
                    new TestReward { itemId = 1, count = 10 },
                },
            },
            new TestMail
            {
                id = 2,
                status = 0,
                title = "测试邮件2",
                content = "这是一封测试邮件",
                rewards = new List<TestReward>(),
            },
        };
    }

    private static void WriteMailList(List<byte> b, List<TestMail> mails)
    {
        WriteInt(b, mails.Count);
        foreach (var mail in mails)
        {
            WriteUInt(b, mail.id);
            WriteInt(b, 0);                        // type
            WriteUInt(b, mail.status);
            WriteLong(b, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            WriteString(b, mail.title);
            WriteString(b, mail.content);
            WriteInt(b, mail.rewards.Count);

            foreach (var reward in mail.rewards)
            {
                WriteUInt(b, reward.itemId);
                WriteUInt(b, reward.count);
            }
        }
    }

    private static string ReadString(byte[] data, int offset)
    {
        if (offset + 2 > data.Length) return string.Empty;
        int length = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
        offset += 2;
        if (offset + length > data.Length) return string.Empty;
        return Encoding.UTF8.GetString(data, offset, length);
    }

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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

    private static void WriteUInt(List<byte> b, uint v)
    {
        Span<byte> tmp = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tmp, v);
        b.Add(tmp[0]); b.Add(tmp[1]); b.Add(tmp[2]); b.Add(tmp[3]);
    }

    private static void WriteLong(List<byte> b, long v)
    {
        Span<byte> tmp = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(tmp, v);
        b.Add(tmp[0]); b.Add(tmp[1]); b.Add(tmp[2]); b.Add(tmp[3]);
        b.Add(tmp[4]); b.Add(tmp[5]); b.Add(tmp[6]); b.Add(tmp[7]);
    }

    private static void WriteString(List<byte> b, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        WriteUShort(b, (ushort)bytes.Length);
        b.AddRange(bytes);
    }

    private static void Log(string message)
        => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
}