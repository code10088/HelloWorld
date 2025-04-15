using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class STCP : SInterface
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private Func<byte[], bool> deserialize;
    private Queue<TcpSendItem> sendPool = new Queue<TcpSendItem>();

    private bool connectMark = false;
    private bool sendMark = false;
    private bool receiveMark = false;
    private byte[] receiveBuffer = new byte[1024];
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public void Init(string ip, ushort port, uint connectId, Func<byte[], bool> deserialize)
    {
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        this.deserialize = deserialize;
        Connect();
    }

    #region 连接
    private void Connect()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.BeginConnect(endPoint, ConnectCallback, null);
    }
    private void ConnectCallback(IAsyncResult ar)
    {
        socket.EndConnect(ar);
        if (ar.IsCompleted)
        {
            connectMark = true;
            sendMark = true;
            receiveMark = true;
            thread = new Thread(Handle);
            thread.Start();
        }
        else
        {
            Reconect();
        }
    }
    /// <summary>
    /// 断线重连
    /// </summary>
    private void Reconect()
    {
        Disconnect();
        Connect();
    }
    public void Disconnect()
    {
        connectMark = false;//会影响线程中断
        thread?.Join();
        socket.Disconnect(false);
        socket.Close();
        socket = null;
        sendPool.Clear();
        sendMark = false;
        receiveMark = false;
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    private void Handle()
    {
        while (connectMark)
        {
            Send();
            Receive();
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    class TcpSendItem
    {
        public ushort id;
        public IExtensible msg;
    }
    public void Send(ushort id, IExtensible msg)
    {
        TcpSendItem tsi = new TcpSendItem() { id = id, msg = msg };
        lock (sendPool) sendPool.Enqueue(tsi);
    }
    private void Send()
    {
        if (!sendMark && sendPool.Count == 0) return;
        TcpSendItem tsi;
        lock (sendPool) tsi = sendPool.Dequeue();
        byte[] bytes = Serialize(tsi.id, tsi.msg);
        socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, null);
        sendMark = false;
    }
    private void SendCallback(IAsyncResult ar)
    {
        int sendLength = socket.EndSend(ar);
        if (sendLength > 0)
        {
            sendMark = true;
        }
        else
        {
            Reconect();
        }
    }
    /// <summary>
    /// Array.Copy < Buffer.BlockCopy < Buffer.MemoryCopy
    /// </summary>
    private byte[] Serialize(ushort id, IExtensible msg)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Serializer.Serialize(ms, msg);
            byte[] source = ms.ToArray();
            int l = source.Length;
            byte[] result = new byte[6 + l];
            //消息长度
            byte[] temp = BitConverter.GetBytes(2 + l);
            Buffer.BlockCopy(temp, 0, result, 0, 4);
            //消息ID
            temp = BitConverter.GetBytes(id);
            Buffer.BlockCopy(temp, 0, result, 4, 2);
            //消息内容
            Buffer.BlockCopy(source, 0, result, 6, l);
            return result;
        }
    }
    #endregion

    #region 接收
    private void Receive()
    {
        if (!receiveMark) return;
        socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        receiveMark = false;
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        int receiveLength = socket.EndReceive(ar);
        if (receiveLength > 0)
        {
            Parse(receiveLength);
            receiveMark = true;
        }
        else
        {
            Reconect();
        }
    }
    private void Parse(int receiveLength)
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
                    if (bodyLength >= 0 && bodyLength <= 0x100000)
                    {
                        bodyBuffer = new byte[bodyLength];
                    }
                    else
                    {
                        headPos = 0;
                        bodyPos = 0;
                        bodyLength = 0;
                        return;
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
                    if (!deserialize(bodyBuffer)) return;
                }
            }
        }
    }
    #endregion
}