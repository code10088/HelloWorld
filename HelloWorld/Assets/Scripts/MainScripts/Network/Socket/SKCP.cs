#if !UNITY_WEBGL
using System;
using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

public class SKCP : SBase
{
    private Thread sendThread;
    private Thread receiveThread;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private DateTimeOffset next;
    private SendItem kcpConnect;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        kcpSend = new KcpSend(Send);
        var msg = new CS_KcpConnect();
        msg.playerId = playerId;
        msg.Token = token;
        kcpConnect = new SendItem(NetMsgId.CSKcpConnect, msg);
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override void Connect()
    {
        Task.Run(ConnectAsync);
    }
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> ConnectAsync()
    {
        if (await base.ConnectAsync() == false) return false;
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return false;
        }
        socket.Connect(SocketType.Dgram, ProtocolType.Udp);
        var stream = kcpConnect.Serialize();
        while (true)
        {
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
                ConnectAsync();
                return false;
            }
        }
        while (true)
        {
            int count = socket.Receive(receiveBuffer);
            if (count == 6 && BitConverter.ToUInt16(receiveBuffer) == NetMsgId.SCKcpConnect)
            {
                socketevent.Invoke((int)SocketEvent.Connected, 0);
                var connectId = BitConverter.ToUInt32(receiveBuffer, 2);
                kcp = new PoolSegManager.Kcp(connectId, kcpSend);
                kcp.NoDelay(1, 10, 2, 1);
                kcp.WndSize();
                kcp.SetMtu();
                next = DateTime.UtcNow;

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
                ConnectAsync();
                return false;
            }
        }
    }
    public override async Task Close()
    {
        await base.Close();
        kcp?.Dispose();
        kcp = null;
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
    }
    #endregion

    #region 发送
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
                var stream = item.Serialize();
                kcp.Send(stream.Buffer.AsSpan(0, stream.WPos));
                stream.Dispose();
            }
            var current = DateTime.UtcNow;
            if (current >= next)
            {
                kcp.Update(current);
                next = kcp.Check(current);
            }
            var ms = (next - current).TotalMilliseconds;
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
                ConnectAsync();
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
            if (count >= 0 && Deserialize(receiveBuffer, count))
            {
                receiveRetry = 0;
                Thread.Sleep(GameSetting.updateTimeSliceMS);
                continue;
            }
            if (receiveRetry++ > 0)
            {
                ConnectAsync();
                return;
            }
        }
    }
    private bool Deserialize(byte[] buffer, int length)
    {
        kcp.Input(buffer.AsSpan(0, length));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) return true;
            size = kcp.Recv(buffer);
            if (size <= 0) return true;
            if (!Receive(buffer, size)) return false;
        }
    }
    #endregion
}
#endif