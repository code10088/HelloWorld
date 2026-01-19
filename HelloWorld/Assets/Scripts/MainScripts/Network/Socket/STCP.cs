using ProtoBuf;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private Task receiveTask;

    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        base.Init(ip, port, connectId, deserialize, socketevent);
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        Connect();
    }

    #region 连接
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.SendTimeout = heartInterval;
        socket.ReceiveTimeout = heartInterval;
        try
        {
            await socket.ConnectAsync(endPoint);
        }
        catch
        {

        }
        if (socket.Connected)
        {
            connectMark = true;
            connectRetry = 0;
            sendRetry = 0;
            receiveRetry = 0;
            signal = new SemaphoreSlim(0);
            cts = new CancellationTokenSource();
            sendTask = Send(cts.Token);
            receiveTask = Receive(cts.Token);
            socketevent.Invoke((int)SocketEvent.Connected, 0);
            return true;
        }
        else
        {
            Connect();
            return false;
        }
    }
    public override async Task Close()
    {
        await base.Close();
        signal?.Release();
        signal?.Dispose();
        signal = null;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        socket?.Shutdown(SocketShutdown.Both);
        socket?.Close();
        socket?.Dispose();
        socket = null;
        await (sendTask ?? Task.CompletedTask);
        await (receiveTask ?? Task.CompletedTask);
        if (bodyBuffer != null) bytePool.Return(bodyBuffer);
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    #region 发送
    public override void Send(ushort id, IExtensible msg)
    {
        if (connectMark)
        {
            base.Send(id, msg);
            signal?.Release();
        }
    }
    private async Task Send(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await signal.WaitAsync(token);
            }
            catch
            {
                return;
            }
            if (connectMark == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var wb = new WriteBuffer(bytePool, 256, 6);
                Serializer.Serialize(wb, item.msg);
                BinaryPrimitives.WriteInt32LittleEndian(wb.Buffer.AsSpan(0, 4), wb.Pos - 4);
                BinaryPrimitives.WriteUInt16LittleEndian(wb.Buffer.AsSpan(4, 2), item.id);
                while (true)
                {
                    int count = 0;
                    while (count < wb.Pos)
                    {
                        int l = 0;
                        try
                        {
                            //socket使用CancellationToken，cts?.Cancel导致await无法退出
                            l = await socket.SendAsync(wb.Buffer.AsMemory(count, wb.Pos - count), SocketFlags.None);
                        }
                        catch
                        {
                            l = -1;
                        }
                        if (connectMark == false)
                        {
                            wb.Dispose();
                            return;
                        }
                        if (l <= 0)
                        {
                            break;
                        }
                        count += l;
                    }
                    if (count == wb.Pos)
                    {
                        wb.Dispose();
                        sendRetry = 0;
                        break;
                    }
                    if (sendRetry++ > 0)
                    {
                        wb.Dispose();
                        Connect();
                        return;
                    }
                }
            }
        }
    }
    #endregion

    #region 接收
    private async Task Receive(CancellationToken token)
    {
        while (true)
        {
            int count = 0;
            try
            {
                //socket使用CancellationToken，cts?.Cancel导致await无法退出
                count = await socket.ReceiveAsync(receiveBuffer.AsMemory(), SocketFlags.None);
            }
            catch
            {
                count = -1;
            }
            if (connectMark == false)
            {
                return;
            }
            if (count >= 0 && Deserialize(count))
            {
                receiveRetry = 0;
                continue;
            }
            if (receiveRetry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    private bool Deserialize(int receiveLength)
    {
        int receivePos = 0;
        while (receivePos < receiveLength)
        {
            if (headPos < headLength)
            {
                int l = Math.Min(headLength - headPos, receiveLength - receivePos);
                Buffer.BlockCopy(receiveBuffer, receivePos, headBuffer, headPos, l);
                receivePos += l;
                headPos += l;
                if (headPos == headLength)
                {
                    bodyLength = BitConverter.ToInt32(headBuffer, 0);
                    if (bodyLength >= 0 && bodyLength <= 0x2800)
                    {
                        bodyBuffer = bytePool.Rent(bodyLength);
                    }
                    else
                    {
                        headPos = 0;
                        bodyPos = 0;
                        bodyLength = 0;
                        return false;
                    }
                }
            }
            else
            {
                int l = Math.Min(bodyLength - bodyPos, receiveLength - receivePos);
                Buffer.BlockCopy(receiveBuffer, receivePos, bodyBuffer, bodyPos, l);
                receivePos += l;
                bodyPos += l;
                if (bodyPos == bodyLength)
                {
                    headPos = 0;
                    bodyPos = 0;
                    bool b = Deserialize(bodyBuffer, bodyLength);
                    bytePool.Return(bodyBuffer);
                    bodyBuffer = null;
                    bodyLength = 0;
                    if (!b) return false;
                }
            }
        }
        return true;
    }
    #endregion
}