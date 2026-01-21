using ProtoBuf;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class HTCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread sendThread;
    private Thread receiveThread;

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
            socket.Connect(endPoint);
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
            sendThread = new Thread(Send);
            sendThread.Start();
            receiveThread = new Thread(Receive);
            receiveThread.Start();
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
        socket?.Close();
        socket?.Dispose();
        socket = null;
        await Task.Yield();
        sendThread?.Join();
        receiveThread?.Join();
        if (bodyBuffer != null) bytePool.Return(bodyBuffer);
        bodyBuffer = null;
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
                            l = socket.Send(wb.Buffer, count, wb.Pos - count, SocketFlags.None);
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
            Thread.Sleep(GameSetting.updateTimeSliceMS);
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