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

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        kcpSend = new KcpSend(Send);
        var msg = new CS_KcpConnect();
        msg.playerId = playerId;
        msg.token = token;
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
    private async Task ConnectAsync()
    {
        Close();
        if (connectRetry++ > 0)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        socket.Connect(SocketType.Dgram, ProtocolType.Udp);
        var buffer = kcpConnect.Serialize();
        while (true)
        {
            int count = socket.Send(buffer.Span);
            if (count == buffer.WPos)
            {
                UnsafeByteBuffer.Return(buffer);
                sendRetry = 0;
                break;
            }
            if (sendRetry++ > 0)
            {
                UnsafeByteBuffer.Return(buffer);
                Connect();
                return;
            }
        }
        while (true)
        {
            int count = socket.Receive(receiveBuffer.FullSpan);
            if (count == 6)
            {
                receiveBuffer.SetWPos(6);
                receiveBuffer.SetRPos(0);
                if (receiveBuffer.ReadUShort() == NetMsgId.SCKcpConnect)
                {
                    socketevent.Invoke((int)SocketEvent.Connected, 0);
                    var connectId = receiveBuffer.ReadUInt();
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
                    return;
                }
            }
            if (count >= 0)
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
    public override void Close()
    {
        base.Close();
        kcp?.Dispose();
        kcp = null;
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
                var buffer = item.Serialize();
                kcp.Send(buffer.Span);
                UnsafeByteBuffer.Return(buffer);
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
            int count = socket.Receive(receiveBuffer.FullSpan);
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
                Connect();
                return;
            }
        }
    }
    private bool Deserialize(UnsafeByteBuffer buffer, int length)
    {
        kcp.Input(buffer.FullSpan.Slice(0, length));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) return true;
            size = kcp.Recv(buffer.FullSpan);
            if (size <= 0) return true;
            buffer.SetWPos(size);
            buffer.SetRPos(0);
            if (!Receive(buffer)) return false;
        }
    }
    #endregion
}
#endif