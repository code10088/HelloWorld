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

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        kcp = new KcpHandle(connectId, Send, Receive);
        base.Init(ip, port, connectId, deserialize, socketevent);
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        kcp.Start();
        socket.Connect(SocketType.Dgram, ProtocolType.Udp);
        connectMark = true;
        sendRetry = 0;
        receiveRetry = 0;
        sendThread = new Thread(Send);
        sendThread.Start();
        receiveThread = new Thread(Receive);
        receiveThread.Start();
        socketevent.Invoke((int)SocketEvent.Connected, 0);
        return true;
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
                var wb = serialize.Serialize(item.Id, item.Msg);
                kcp.Send(wb.Buffer.AsSpan(0, wb.Pos));
                wb.Dispose();
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
            if (count >= 0 && kcp.Deserialize(receiveBuffer, count, ref connectRetry))
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