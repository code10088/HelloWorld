using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class STCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private bool threadMark = true;

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
        thread = new Thread(Update);
        thread.Start();
        Connect();
    }
    private void Update()
    {
        while (threadMark)
        {
            Send();
            Receive();
            UpdateHeart(GameSetting.updateTimeSliceMS);
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 连接
    public override void Connect()
    {
        connectMark = false;
        if (CheckNetworkNotReachable())
        {
            return;
        }
        if (socket == null)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = heartInterval;
            socket.ReceiveTimeout = heartInterval;
        }
        else
        {
            socket.Disconnect(true);
        }
        socket.Connect(endPoint);
        if (socket.Connected)
        {
            connectMark = true;
            connectRetry = 0;
            sendRetry = 0;
            receiveMark = true;
            receiveRetry = 0;
        }
        else
        {
            Reconnect();
        }
    }
    private void Reconnect()
    {
        connectRetry++;
        if (connectRetry == 1)
        {
            Connect();
            return;
        }
        if (connectRetry == 2)
        {
            socket.Disconnect(false);
            socket.Close();
            socket = null;
            Connect();
            return;
        }
        socketevent.Invoke((int)SocketEvent.ConnectError, 0);
    }
    public override void Close()
    {
        base.Close();
        threadMark = false;
        thread?.Join();
        socket.Disconnect(false);
        socket.Close();
        socket = null;
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
        while (connectMark && sendQueue.Count > 0)
        {
            SendItem item;
            lock (sendQueue) item = sendQueue.Dequeue();
            Send(Serialize(item.id, item.msg));
        }
    }
    private async Task Send(byte[] bytes)
    {
        int count = 0;
        try
        {
            count = await socket.SendAsync(bytes.AsMemory(), SocketFlags.None);
        }
        catch
        {
            count = -1;
        }
        if (connectMark == false)
        {
            bytePool.Return(bytes);
            return;
        }
        if (count == bytes.Length)
        {
            sendRetry = 0;
            bytePool.Return(bytes);
            return;
        }
        if (sendRetry++ < 1)
        {
            Send(bytes);
            return;
        }
        bytePool.Return(bytes);
        Reconnect();
    }
    #endregion

    #region 接收
    private async Task Receive()
    {
        if (receiveMark == false)
        {
            return;
        }
        receiveMark = false;
        int count = 0;
        try
        {
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
        if (Deserialize(count))
        {
            receiveRetry = 0;
            receiveMark = true;
            return;
        }
        if (receiveRetry++ < 1)
        {
            receiveMark = true;
            return;
        }
        Reconnect();
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
                    bodyLength = 0;
                    bool b = Deserialize(bodyBuffer);
                    bytePool.Return(bodyBuffer);
                    bodyBuffer = null;
                    if (!b) return false;
                }
            }
        }
        return true;
    }
    #endregion
}