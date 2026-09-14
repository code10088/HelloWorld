#if !UNITY_WEBGL
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private Thread sendThread;
    private Thread receiveThread;
    private UnsafeByteBuffer headBuffer;
    private UnsafeByteBuffer bodyBuffer;
    private int headLength = 4;
    private int bodyLength = 0;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        headBuffer = UnsafeByteBuffer.Rent(4);
        bodyBuffer = UnsafeByteBuffer.Rent(2048);
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override void Connect()
    {
        Task.Run(ConnectAsync);
    }
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
        socket.Connect(SocketType.Stream, ProtocolType.Tcp);
        if (socket.Connected)
        {
            socketevent.Invoke((int)SocketEvent.Connected, 0);
            Connected = true;
            connectRetry = 0;
            sendThread = new Thread(Send);
            sendThread.IsBackground = true;
            sendThread.Start();
            receiveThread = new Thread(Receive);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            heart.Start();
        }
        else
        {
            Connect();
        }
    }
    public override void Close()
    {
        base.Close();
        sendThread?.Join();
        receiveThread?.Join();
        headBuffer?.Clear();
        bodyBuffer?.Clear();
        bodyLength = 0;
    }
    public override void Dispose()
    {
        base.Dispose();
        UnsafeByteBuffer.Return(headBuffer);
        headBuffer = null;
        UnsafeByteBuffer.Return(bodyBuffer);
        bodyBuffer = null;
    }
    #endregion

    #region 发送
    private void Send()
    {
        int retry = 0;
        while (true)
        {
            if (Connected == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var buffer = item.Serialize(true);
                while (true)
                {
                    int count = socket.Send(buffer.Span, buffer.WPos);
                    if (Connected == false)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        return;
                    }
                    if (count == buffer.WPos)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        retry = 0;
                        break;
                    }
                    if (retry++ > 0)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        Connect();
                        return;
                    }
                }
            }
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }
    #endregion

    #region 接收
    private void Receive()
    {
        int retry = 0;
        while (true)
        {
            int count = socket.Receive(receiveBuffer.FullSpan);
            if (Connected == false)
            {
                return;
            }
            if (count > 0 && Deserialize(receiveBuffer, count))
            {
                retry = 0;
                Thread.Sleep(GameSetting.updateTimeSliceMS);
                continue;
            }
            if (count == 0 || retry++ > 0)
            {
                Connect();
                return;
            }
        }
    }
    private bool Deserialize(UnsafeByteBuffer buffer, int length)
    {
        int pos = 0;
        while (pos < length)
        {
            if (headBuffer.WPos < headLength)
            {
                int l = Math.Min(headLength - headBuffer.WPos, length - pos);
                headBuffer.WriteBuffer(buffer, pos, l);
                pos += l;
                if (headBuffer.WPos == headLength)
                {
                    headBuffer.SetRPos(0);
                    bodyLength = headBuffer.ReadInt();
                    if (bodyLength < 0 || bodyLength > bodyBuffer.Capacity)
                    {
                        headBuffer.Clear();
                        bodyBuffer.Clear();
                        bodyLength = 0;
                        return false;
                    }
                }
            }
            else
            {
                int l = Math.Min(bodyLength - bodyBuffer.WPos, length - pos);
                bodyBuffer.WriteBuffer(buffer, pos, l);
                pos += l;
                if (bodyBuffer.WPos == bodyLength)
                {
                    bool b = Receive(bodyBuffer);
                    headBuffer.Clear();
                    bodyBuffer.Clear();
                    bodyLength = 0;
                    if (!b) return false;
                }
            }
        }
        return true;
    }
    #endregion
}
#endif
