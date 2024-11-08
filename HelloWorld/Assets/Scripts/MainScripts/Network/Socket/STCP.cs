using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class STCP
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private Queue<TcpSendItem> sendPool = new Queue<TcpSendItem>();

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
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        Connect();
    }

    #region 连接
    private void Connect()
    {
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
            Send();
            Receive();
            if (!connectMark) return;
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    public void Send(ushort id, IExtensible msg)
    {
        TcpSendItem nmb = new TcpSendItem(id, msg);
        lock (sendPool) sendPool.Enqueue(nmb);
    }
    private void Send()
    {
        while (sendPool.Count > 0)
        {
            if (!connectMark)
            {
                return;
            }
            lock (sendPool)
            {
                TcpSendItem nmb = sendPool.Dequeue();
                nmb.Send(socket, Send);
            }
        }
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
    #endregion

    #region 接收
    private void Receive()
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
                    bool success = Deserialize(bodyBuffer);
                    if (!success) return;
                    headPos = 0;
                    bodyPos = 0;
                }
            }
        }
    }
    private bool Deserialize(byte[] bytes)
    {
        bool success = SocketManager.Instance.Deserialize(bytes);
        if (!success) Reconect();
        return success;
    }
    #endregion

    class TcpSendItem
    {
        private ushort id;
        private IExtensible msg;
        private Socket socket;
        private Action<bool, TcpSendItem> finish;
        private int sendTimer = -1;
        private int retryTime;
        public TcpSendItem(ushort id, IExtensible msg)
        {
            this.id = id;
            this.msg = msg;
        }
        public void Send(Socket socket, Action<bool, TcpSendItem> finish)
        {
            this.socket = socket;
            this.finish = finish;
            byte[] bytes = Serialize();
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, null);
            if (sendTimer < 0) sendTimer = TimeManager.Instance.StartTimer(10, finish: () => SendCallback(false));
        }
        private byte[] Serialize()
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
        private void SendCallback(IAsyncResult ar)
        {
            TimeManager.Instance.StopTimer(sendTimer);
            int sendLength = socket.EndSend(ar);
            SendCallback(sendLength > 0);
        }
        private void SendCallback(bool result)
        {
            if (result)
            {
                retryTime = 0;
                finish(true, this);
            }
            else if (retryTime < 3)
            {
                retryTime++;
                finish(false, this);
            }
            else
            {
                retryTime = 0;
                GameDebug.LogError("消息发送失败:" + this);
            }
        }
    }
}