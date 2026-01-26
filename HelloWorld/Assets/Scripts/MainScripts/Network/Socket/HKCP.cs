using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class HKCP : SBase
{
    private KcpHandle kcp;
    private Thread sendThread;
    private Thread receiveThread;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        kcp = new KcpHandle(playerId, token, Send, Receive);
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket.Connect(SocketType.Dgram, ProtocolType.Udp);
        var stream = serialize.Serialize(0, kcp.CS_KcpConnect);
        while (true)
        {
            //await不受socket超时影响会一直等待
            //int count = await socket.SendAsync(stream.Buffer.AsMemory(0, stream.Pos));
            int count = socket.Send(stream.Buffer.AsSpan(0, stream.WPos));
            if (count == stream.WPos)
            {
                stream.Dispose();
                sendRetry = 0;
                break;
            }
            if (sendRetry++ > 0)
            {
                stream.Dispose();
                Connect();
                return false;
            }
        }
        while (true)
        {
            //await不受socket超时影响会一直等待
            //int count = await socket.ReceiveAsync(receiveBuffer);
            int count = socket.Receive(receiveBuffer);
            if (count == 6 && BitConverter.ToUInt16(receiveBuffer) == NetMsgId.KcpConnect)
            {
                socketevent.Invoke((int)SocketEvent.Connected, 0);
                var connectId = BitConverter.ToUInt32(receiveBuffer, 2);
                kcp.Start(connectId);
                connectMark = true;
                connectRetry = 0;
                sendRetry = 0;
                receiveRetry = 0;
                sendThread = new Thread(Send);
                sendThread.Start();
                receiveThread = new Thread(Receive);
                receiveThread.Start();
                heart.Start();
                return true;
            }
            if (count >= 0)
            {
                receiveRetry = 0;
                continue;
            }
            if (receiveRetry++ > 0)
            {
                Connect();
                return false;
            }
        }
    }
    public override async Task Close()
    {
        await base.Close();
        kcp.Dispose();
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
    }
    #endregion

    #region 发送
    private void Send()
    {
        while (true)
        {
            if (connectMark == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var stream = serialize.Serialize(item.Id, item.Msg);
                kcp.Send(stream.Buffer.AsSpan(0, stream.WPos));
                stream.Dispose();
            }
            var ms = kcp.Update();
            var delay = (int)Math.Clamp(ms, 1, GameSetting.updateTimeSliceMS);
            Thread.Sleep(delay);
        }
    }
    /// <summary>
    /// kcp.Update中执行
    /// </summary>
    private void Send(IMemoryOwner<byte> owner, int length)
    {
        if (connectMark == false)
        {
            owner.Dispose();
            return;
        }
        while (true)
        {
            int count = socket.Send(owner.Memory.Span.Slice(0, length));
            if (connectMark == false)
            {
                owner.Dispose();
                return;
            }
            if (count == length)
            {
                owner.Dispose();
                sendRetry = 0;
                break;
            }
            if (sendRetry++ > 0)
            {
                owner.Dispose();
                Connect();
                return;
            }
        }
    }
    #endregion

    #region 接收
    private void Receive()
    {
        while (true)
        {
            int count = socket.Receive(receiveBuffer);
            if (connectMark == false)
            {
                return;
            }
            if (count >= 0 && kcp.Deserialize(receiveBuffer, count))
            {
                receiveRetry = 0;
                Thread.Sleep(GameSetting.updateTimeSliceMS);
                continue;
            }
            if (receiveRetry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    #endregion
}