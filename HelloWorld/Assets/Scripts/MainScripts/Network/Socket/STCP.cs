#if !UNITY_WEBGL
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private Thread sendThread;
    private Thread receiveThread;
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer = new byte[2048];
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    #region 连接
    protected override void Connect()
    {
        Task.Run(ConnectAsync);
    }
    protected override async Task<bool> ConnectAsync()
    {
        if (await base.ConnectAsync() == false) return false;
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
        headPos = 0;
        bodyPos = 0;
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
                var stream = item.Serialize();
                while (true)
                {
                    int count = socket.Send(stream.Buffer, stream.WPos);
                    if (connectMark == false)
                    {
                        stream.Dispose();
                        return;
                    }
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
        int pos = 0;
        while (pos < length)
        {
            if (headPos < headLength)
            {
                int l = Math.Min(headLength - headPos, length - pos);
                Buffer.BlockCopy(buffer, pos, headBuffer, headPos, l);
                pos += l;
                headPos += l;
                if (headPos == headLength)
                {
                    bodyLength = BitConverter.ToInt32(headBuffer, 0);
                    if (bodyLength < 0 || bodyLength > bodyBuffer.Length)
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
                int l = Math.Min(bodyLength - bodyPos, length - pos);
                Buffer.BlockCopy(buffer, pos, bodyBuffer, bodyPos, l);
                pos += l;
                bodyPos += l;
                if (bodyPos == bodyLength)
                {
                    headPos = 0;
                    bodyPos = 0;
                    bool b = Receive(bodyBuffer, bodyLength);
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