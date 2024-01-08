using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class STCP
{
    private string ip;
    private ushort port;
    private Socket socket;
    private Thread thread;
    private Queue<TcpSendItem> sendPool = new Queue<TcpSendItem>();
    private Queue<byte[]> receivePool = new Queue<byte[]>();

    private int connectTimer = -1;
    private int receiveTimer = -1;
    private int sendFailCount = 0;
    private int receiveFailCount = 0;
    private bool receiveMark = false;
    private bool connectMark = false;
    private byte[] receiveBuffer = new byte[1024];
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public void Init(string ip, ushort port)
    {
        this.ip = ip;
        this.port = port;
        Connect();
    }

    #region 连接
    private void Connect()
    {
        IPAddress address = IPAddress.Parse(ip);
        IPEndPoint endPoint = new IPEndPoint(address, port);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.BeginConnect(endPoint, ConnectCallback, null);
        if (connectTimer < 0) connectTimer = TimeManager.Instance.StartTimer(10, finish: Reconect);
    }
    private void ConnectCallback(IAsyncResult ar)
    {
        TimeManager.Instance.StopTimer(connectTimer);
        socket.EndConnect(ar);
        if (ar.IsCompleted)
        {
            receiveMark = true;
            connectMark = true;
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
        receivePool.Clear();
        TimeManager.Instance.StopTimer(connectTimer);
        TimeManager.Instance.StopTimer(receiveTimer);
        sendFailCount = 0;
        receiveFailCount = 0;
        receiveMark = false;
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    private void Handle()
    {
        while (true)
        {
            if (!connectMark) return;
            while (sendPool.Count > 0)
            {
                if (!connectMark)
                {
                    return;
                }
                lock (sendPool)
                {
                    TcpSendItem nmb = sendPool.Dequeue();
                    nmb.Send(this);
                }
            }
            BeginReceive();
            while (receivePool.Count > 0)
            {
                if (!connectMark)
                {
                    return;
                }
                lock (receivePool)
                {
                    byte[] bytes = receivePool.Dequeue();
                    Deserialize(bytes);
                }
            }
            if (!connectMark) return;
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    private void BeginSend(byte[] bytes, AsyncCallback callback)
    {
        socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, callback, null);
    }
    private int EndSend(IAsyncResult ar)
    {
        return socket.EndSend(ar);
    }
    private void Send(bool result, TcpSendItem nmb)
    {
        if (result)
        {
            sendFailCount = 0;
        }
        else if (sendFailCount < 3)
        {
            sendFailCount++;
            lock (sendPool) sendPool.Enqueue(nmb);
        }
        else
        {
            Reconect();
        }
    }
    public void Send(ushort id, IExtensible msg)
    {
        TcpSendItem nmb = new TcpSendItem(id, msg);
        lock (sendPool) sendPool.Enqueue(nmb);
    }
    #endregion

    #region 接收
    private void BeginReceive()
    {
        if (receiveMark)
        {
            receiveMark = false;
            socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
            if (receiveTimer < 0) receiveTimer = TimeManager.Instance.StartTimer(10, finish: () => ReceiveCallback(0));
        }
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        if (!receiveMark)
        {
            TimeManager.Instance.StopTimer(receiveTimer);
            int receiveLength = socket.EndReceive(ar);
            ReceiveCallback(receiveLength);
        }
    }
    private void ReceiveCallback(int receiveLength)
    {
        if (receiveLength > 0)
        {
            Parse(receiveLength);
            receiveFailCount = 0;
            receiveMark = true;
        }
        else if (receiveFailCount < 3)
        {
            receiveFailCount++;
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
                    bodyBuffer = new byte[bodyLength];
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
                    lock (receivePool) receivePool.Enqueue(bodyBuffer);
                    headPos = 0;
                    bodyPos = 0;
                }
            }
        }
    }
    private void Deserialize(byte[] bytes)
    {
        bool success = NetMsgDispatch.Instance.Deserialize(bytes);
        if (!success) Reconect();
    }
    #endregion

    class TcpSendItem
    {
        private STCP so;
        private int sendTimer = -1;
        private int retryTime;
        private ushort id;
        private IExtensible msg;
        public TcpSendItem(ushort id, IExtensible msg)
        {
            this.id = id;
            this.msg = msg;
        }
        public void Send(STCP so)
        {
            this.so = so;
            so.BeginSend(Serialize(msg), SendCallback);
            if (sendTimer < 0) sendTimer = TimeManager.Instance.StartTimer(10, finish: () => SendCallback(false));
        }
        private byte[] Serialize(IExtensible msg)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, msg);
                byte[] source = ms.ToArray();
                int l = source.Length;
                byte[] result = new byte[6 + l];
                //消息长度
                byte[] temp = BitConverter.GetBytes(l);
                Buffer.BlockCopy(temp, 0, result, 0, 4);
                //消息ID
                temp = BitConverter.GetBytes(id);
                Buffer.BlockCopy(temp, 0, result, 4, 2);
                //消息内容
                Buffer.BlockCopy(source, 0, result, 6, l);
                return result;
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            TimeManager.Instance.StopTimer(sendTimer);
            int sendLength = so.EndSend(ar);
            SendCallback(sendLength > 0);
        }
        private void SendCallback(bool result)
        {
            if (result)
            {
                retryTime = 0;
                so.Send(true, this);
            }
            else if (retryTime < 3)
            {
                retryTime++;
                so.Send(false, this);
            }
            else
            {
                retryTime = 0;
                GameDebug.LogError("消息发送失败:" + this);
            }
        }
    }
}