using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using WebSocketSharp;

public class SWeb : SInterface
{
    private string ip;
    private WebSocket socket;
    private Func<byte[], bool> deserialize;
    private Queue<WebSendItem> sendPool = new Queue<WebSendItem>();

    private int updateId = -1;
    private bool connectMark = false;
    private bool sendMark = false;
    private byte[] receiveBuffer;
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public void Init(string ip, ushort port, uint connectId, Func<byte[], bool> deserialize)
    {
        this.ip = $"{ip}:{port}";
        this.deserialize = deserialize;
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
    }
    private void ConnectCallback(object sender, EventArgs e)
    {
        connectMark = true;
        sendMark = true;
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(Handle);
    }
    private void Error(object sender, WebSocketSharp.ErrorEventArgs error)
    {
        GameDebug.LogError(error.Message);
        Reconect();
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
        connectMark = false;
        sendMark = false;
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    private void Handle(float t)
    {
        if (connectMark) Send();
    }

    #region 发送
    class WebSendItem
    {
        public ushort id;
        public IExtensible msg;
    }
    public void Send(ushort id, IExtensible msg)
    {
        WebSendItem wsi = new WebSendItem() { id = id, msg = msg };
        lock (sendPool) sendPool.Enqueue(wsi);
    }
    public void Send()
    {
        if (!sendMark || sendPool.Count == 0) return;
        WebSendItem wsi = sendPool.Dequeue();
        byte[] bytes = Serialize(wsi.id, wsi.msg);
        socket.SendAsync(bytes, SendCallback);
        sendMark = false;
    }
    private void SendCallback(bool result)
    {
        sendMark = true;
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