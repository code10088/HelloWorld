using ProtoBuf;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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
        threadMark = true;
        thread.Start();
    }
    private void Update()
    {
        Connect();
        while (threadMark)
        {
            Send();
            Receive();
            UpdateHeart(GameSetting.updateTimeSliceMS);
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 连接
    private void Connect()
    {
        connectMark = false;
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        if (socket == null)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = heartInterval;
            socket.ReceiveTimeout = heartInterval;
        }
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
            receiveMark = true;
            receiveRetry = 0;
            socketevent.Invoke((int)SocketEvent.Connected, 0);
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
            try { socket.Disconnect(true); }
            catch { }
            Connect();
            return;
        }
        if (connectRetry == 2)
        {
            socket.Close();
            socket = null;
            Connect();
            return;
        }
        socketevent.Invoke((int)SocketEvent.ConnectError, 0);
        Close();
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
            var bytes = Serialize(item.id, item.msg, out int length);
            Send(bytes, length);
        }
    }
    /// <summary>
    /// Array.Copy < Buffer.BlockCopy < Buffer.MemoryCopy
    /// </summary>
    private byte[] Serialize(ushort id, IExtensible msg, out int length)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Serializer.Serialize(ms, msg);
            length = 6 + (int)ms.Length;
            byte[] result = bytePool.Rent(length);
            //消息长度
            byte[] temp = BitConverter.GetBytes(length - 4);
            Buffer.BlockCopy(temp, 0, result, 0, 4);
            //消息ID
            temp = BitConverter.GetBytes(id);
            Buffer.BlockCopy(temp, 0, result, 4, 2);
            //消息内容
            ms.Read(result, 6, length - 6);
            return result;
        }
    }
    private async Task Send(byte[] bytes, int length)
    {
        int count = 0;
        try
        {
            count = await socket.SendAsync(bytes.AsMemory(0, length), SocketFlags.None);
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
        if (count == length)
        {
            sendRetry = 0;
            bytePool.Return(bytes);
            return;
        }
        if (sendRetry++ < 1)
        {
            Send(bytes, length);
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
        if (count >= 0 && Deserialize(count))
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