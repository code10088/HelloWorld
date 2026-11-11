// 测试用 TCP 游戏服务器
// 帧格式：[int32 bodyLength][uint16 id][payload]  （小端）
// 只实现 AIServerTest.txt 中 TCP 部分，客户端 GameStart.cs 使用的就是 TcpChannel。
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal static class Program
{
    private const int DefaultPort = 8088;
    private const int MaxBodyLength = 16 * 1024;

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

    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        int port = int.TryParse(Environment.GetEnvironmentVariable("GAME_PORT"), out var p) ? p : DefaultPort;

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Log($"TCP 游戏服务器已启动，监听 0.0.0.0:{port}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Log($"Accept 异常: {e.Message}");
                    continue;
                }

                client.NoDelay = true;
                var endpoint = client.Client.RemoteEndPoint;
                Log($"客户端已连接: {endpoint}");
                _ = Task.Run(() => HandleClientAsync(client, endpoint));
            }
        }
        finally
        {
            listener.Stop();
            Log("TCP 游戏服务器已停止");
        }
    }

    private static async Task HandleClientAsync(TcpClient client, EndPoint endpoint)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            var head = new byte[4];
            var body = new byte[MaxBodyLength];
            var mails = CreateMails();

            try
            {
                while (true)
                {
                    await ReadExactAsync(stream, head, 0, 4);
                    int bodyLength = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(0, 4));
                    if (bodyLength < 2 || bodyLength > MaxBodyLength)
                    {
                        Log($"[{endpoint}] 非法包长: {bodyLength}");
                        return;
                    }

                    await ReadExactAsync(stream, body, 0, bodyLength);

                    ushort id = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(0, 2));
                    Log($"[{endpoint}] 收到 TCP 消息 id={id}, bodyLength={bodyLength}");

                    switch (id)
                    {
                        case Message_CSAuth:
                            {
                                string token = ReadString(body, 2, bodyLength);
                                Log($"[{endpoint}] 鉴权 token={token}");

                                // 测试鉴权失败：把 code 改成 0
                                await SendFrameAsync(stream, Message_SCAuth, b =>
                                {
                                    WriteInt(b, 1);              // code: 1=成功
                                    WriteLong(b, NowMs());       // 服务器时间
                                });

                                // 鉴权成功后再下发玩家数据，客户端会触发 RefreshPlayerData 进入主场景
                                await SendFrameAsync(stream, Message_SCPlayerData, WritePlayerData);
                            }
                            break;

                        case Message_CSHeart:
                            await SendFrameAsync(stream, Message_SCHeart, b => WriteLong(b, NowMs()));
                            break;

                        case Message_CSMail:
                            await SendFrameAsync(stream, Message_SCMail, b => WriteMailList(b, mails));
                            break;

                        case Message_CSReadMail:
                            {
                                int count = bodyLength >= 6 ? ReadInt(body, 2) : 0;
                                for (int i = 0; i < count; i++)
                                {
                                    int offset = 6 + i * 4;
                                    if (offset + 4 > bodyLength) break;

                                    uint mailId = ReadUInt(body, offset);
                                    var mail = mails.Find(m => m.id == mailId);
                                    if (mail != null && mail.status == 0)
                                        mail.status = 1;
                                }

                                await SendFrameAsync(stream, Message_SCMail, b => WriteMailList(b, mails));
                            }
                            break;

                        case Message_CSGetMailReward:
                            {
                                uint mailId = bodyLength >= 6 ? ReadUInt(body, 2) : 0;
                                var mail = mails.Find(m => m.id == mailId);
                                if (mail != null)
                                    mail.status = 2;

                                await SendFrameAsync(stream, Message_SCGetMailReward, b =>
                                {
                                    WriteInt(b, 0);          // 0=成功
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

                                await SendFrameAsync(stream, Message_SCGetMailAllReward, null);
                            }
                            break;

                        case Message_CSDeleteMail:
                            {
                                uint mailId = bodyLength >= 6 ? ReadUInt(body, 2) : 0;
                                mails.RemoveAll(m => m.id == mailId);

                                await SendFrameAsync(stream, Message_SCDeleteMail, b =>
                                {
                                    WriteInt(b, 0);          // 0=成功
                                    WriteUInt(b, mailId);
                                });
                            }
                            break;

                        case Message_CSDeleteAllMail:
                            mails.Clear();
                            await SendFrameAsync(stream, Message_SCMail, b => WriteInt(b, 0));
                            break;

                        default:
                            Log($"[{endpoint}] 未处理的 TCP 消息 id={id}");
                            break;
                    }
                }
            }
            catch (Exception e) when (e is EndOfStreamException or IOException or SocketException or ObjectDisposedException)
            {
                Log($"[{endpoint}] 连接结束: {e.Message}");
            }
            catch (Exception e)
            {
                Log($"[{endpoint}] 连接异常: {e}");
            }
        }
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
                    new TestReward { itemId = 1001, count = 10 },
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

    private static void WritePlayerData(List<byte> b)
    {
        WriteUInt(b, 10001);                       // playerId
        WriteString(b, "TestPlayer");              // nickName
        WriteString(b, "avatar_01");               // avatar
        WriteUInt(b, 1);                           // level
        WriteLong(b, DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // registerTime
        WriteUInt(b, 0);                           // mail
    }

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(offset, count));
            if (n <= 0) throw new EndOfStreamException();
            offset += n;
            count -= n;
        }
    }

    private static async Task SendFrameAsync(NetworkStream stream, ushort id, Action<List<byte>> writePayload)
    {
        var body = new List<byte>();
        WriteUShort(body, id);
        writePayload?.Invoke(body);

        var packet = new byte[4 + body.Count];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), body.Count);
        body.CopyTo(packet, 4);

        await stream.WriteAsync(packet);
    }

    private static string ReadString(byte[] body, int offset, int bodyLength)
    {
        if (offset + 2 > bodyLength) return string.Empty;
        int length = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(offset, 2));
        offset += 2;
        if (offset + length > bodyLength) return string.Empty;
        return Encoding.UTF8.GetString(body, offset, length);
    }

    private static int ReadInt(byte[] body, int offset)
        => BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(offset, 4));

    private static uint ReadUInt(byte[] body, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(offset, 4));

    private static void WriteUShort(List<byte> b, ushort v)
    {
        b.Add((byte)v);
        b.Add((byte)(v >> 8));
    }

    private static void WriteInt(List<byte> b, int v)
    {
        var tmp = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(tmp, v);
        b.AddRange(tmp);
    }

    private static void WriteUInt(List<byte> b, uint v)
    {
        var tmp = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tmp, v);
        b.AddRange(tmp);
    }

    private static void WriteLong(List<byte> b, long v)
    {
        var tmp = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(tmp, v);
        b.AddRange(tmp);
    }

    private static void WriteString(List<byte> b, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        WriteUShort(b, (ushort)bytes.Length);
        b.AddRange(bytes);
    }

    private static void Log(string message)
        => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
}