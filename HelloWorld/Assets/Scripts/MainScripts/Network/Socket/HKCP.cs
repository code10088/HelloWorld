using ProtoBuf;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class HKCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private uint connectId;
    private KcpSend kcpSend;
    private PoolSegManager.Kcp kcp;
    private Thread sendThread;
    private Thread receiveThread;

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        base.Init(ip, port, connectId, deserialize, socketevent);
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        this.connectId = connectId;
        kcpSend = new KcpSend(Send);
        Connect();
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SendTimeout = heartInterval;
        socket.ReceiveTimeout = heartInterval;
        kcp = new PoolSegManager.Kcp(connectId, kcpSend);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();
        try
        {
            socket.Connect(endPoint);
        }
        catch
        {

        }
        connectMark = true;
        sendRetry = 0;
        receiveRetry = 0;
        sendThread = new Thread(Send);
        sendThread.Start();
        receiveThread = new Thread(Receive);
        receiveThread.Start();
        socketevent.Invoke((int)SocketEvent.Connected, 0);
        UpdateHeart(float.MaxValue);
        return true;
    }
    public override async Task Close()
    {
        await base.Close();
        socket?.Close();
        socket?.Dispose();
        socket = null;
        kcp?.Dispose();
        kcp = null;
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
    }
    #endregion

    #region 发送
    class KcpSend : IKcpCallback
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
        DateTimeOffset next = DateTime.UtcNow;
        while (true)
        {
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
                kcp.Send(wb.Buffer.AsSpan(0, wb.Pos));
                wb.Dispose();
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
        if (connectMark)
        {
            while (true)
            {
                int count = 0;
                try
                {
                    //udp不需要流式发送
                    count = socket.Send(owner.Memory.Span.Slice(0, length), SocketFlags.None);
                }
                catch
                {
                    count = -1;
                }
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
        else
        {
            owner.Dispose();
        }
    }
    #endregion

    #region 接收
    private void Receive()
    {
        while (true)
        {
            int count = 0;
            try
            {
                count = socket.Receive(receiveBuffer, SocketFlags.None);
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
    private bool Deserialize(int receiveLength)
    {
        kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) break;
            var temp = bytePool.Rent(size);
            size = kcp.Recv(temp);
            if (size <= 0)
            {
                bytePool.Return(temp);
                break;
            }
            else
            {
                bool b = Deserialize(temp, size);
                bytePool.Return(temp);
                if (b) connectRetry = 0;
                else return false;
            }
        }
        return true;
    }
    #endregion
}