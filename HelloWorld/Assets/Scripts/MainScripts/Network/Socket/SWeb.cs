using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using WebSocketSharp;

public class SWeb
{
    private string ip;
    private WebSocket socket;
    private Queue<WebSendItem> sendPool = new Queue<WebSendItem>();

    private int connectTimer = -1;
    private int updateId = -1;
    private int sendFailCount = 0;
    private byte[] receiveBuffer = new byte[1024];
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public void Init(string ip, ushort port)
    {
        this.ip = $"{ip}:{port}";
        Connect();
    }

    #region 连接
    private void Connect()
    {
        socket = new WebSocket(ip);
        socket.OnOpen += ConnectCallback;
        socket.OnMessage += Receive;
        socket.OnError += Error;
        socket.ConnectAsync();
        if (connectTimer < 0) connectTimer = TimeManager.Instance.StartTimer(10, finish: Reconect);
    }
    private void ConnectCallback(object sender, EventArgs e)
    {
        TimeManager.Instance.StopTimer(connectTimer);
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(Handle);
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
        socket.Close();
        socket = null;
        sendPool.Clear();
        TimeManager.Instance.StopTimer(connectTimer);
        Updater.Instance.StopUpdate(updateId);
        sendFailCount = 0;
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    private void Handle()
    {
        Send();
    }

    #region 发送
    public void Send(ushort id, IExtensible msg)
    {
        WebSendItem nmb = new WebSendItem(id, msg);
        sendPool.Enqueue(nmb);
    }
    private void Send()
    {
        if (sendPool.Count > 0)
        {
            WebSendItem nmb = sendPool.Dequeue();
            nmb.Send(socket, Send);
        }
    }
    private void Send(bool result, WebSendItem nmb)
    {
        if (result)
        {
            sendFailCount = 0;
        }
        else if (sendFailCount < 3)
        {
            sendFailCount++;
            sendPool.Enqueue(nmb);
        }
        else
        {
            Reconect();
        }
    }
    #endregion

    #region 接收
    private void Receive(object sender, MessageEventArgs message)
    {
        receiveBuffer = message.RawData;
        Parse(receiveBuffer.Length);
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
                    Deserialize(bodyBuffer);
                    headPos = 0;
                    bodyPos = 0;
                }
            }
        }
    }
    private void Deserialize(byte[] bytes)
    {
        bool success = SocketManager.Instance.Deserialize(bytes);
        if (!success) Reconect();
    }
    #endregion

    private void Error(object sender, WebSocketSharp.ErrorEventArgs error)
    {
        GameDebug.LogError(error.Message);
    }

    class WebSendItem
    {
        private ushort id;
        private IExtensible msg;
        private Action<bool, WebSendItem> finish;
        private int sendTimer = -1;
        private int retryTime;
        public WebSendItem(ushort id, IExtensible msg)
        {
            this.id = id;
            this.msg = msg;
        }
        public void Send(WebSocket socket, Action<bool, WebSendItem> finish)
        {
            this.finish = finish;
            byte[] bytes = Serialize();
            socket.SendAsync(bytes, SendCallback);
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
        private void SendCallback(bool result)
        {
            TimeManager.Instance.StopTimer(sendTimer);
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