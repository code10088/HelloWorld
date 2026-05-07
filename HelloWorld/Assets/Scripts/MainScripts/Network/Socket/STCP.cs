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
    protected override async Task<bool> ConnectAsync()
    {
        if (await base.ConnectAsync() == false) return false;
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return false;
        }
        socket.Connect(SocketType.Stream, ProtocolType.Tcp);
        if (socket.Connected)
        {
            socketevent.Invoke((int)SocketEvent.Connected, 0);
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
        else
        {
            ConnectAsync();
            return false;
        }
    }
    public override async Task Close()
    {
        await base.Close();
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
        headBuffer.Clear();
        bodyBuffer.Clear();
        bodyLength = 0;
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
                var buffer = item.Serialize(true);
                while (true)
                {
                    int count = socket.Send(buffer.Span, buffer.WPos);
                    if (connectMark == false)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        return;
                    }
                    if (count == buffer.WPos)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        sendRetry = 0;
                        break;
                    }
                    if (sendRetry++ > 0)
                    {
                        UnsafeByteBuffer.Return(buffer);
                        ConnectAsync();
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
                ConnectAsync();
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