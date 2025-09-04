using ProtoBuf;
using System;
using System.IO;
using System.Net.NetworkInformation;
using WebSocketSharp;

public class SWeb : SBase
{
    private string ip;
    private WebSocket socket;
    private int updateId = -1;
    private bool sendMark = false;
    private byte[] sendBuffer;

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        base.Init(ip, port, connectId, deserialize, socketevent);
        this.ip = $"{ip}:{port}/{connectId}";
        updateId = Updater.Instance.StartUpdate(Update);
        Connect();
    }
    private void Update(float t)
    {
        Send();
        UpdateHeart(t * 1000);
    }

    #region 连接
    private void Connect()
    {
        connectMark = false;
        if (NetworkInterface.GetIsNetworkAvailable() == false)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        if (socket == null)
        {
            socket = new WebSocket(ip);
            socket.OnOpen += ConnectCallback;
            socket.OnMessage += Receive;
            socket.OnError += Error;
        }
        socket.ConnectAsync();//连接失败不会调用ConnectCallback
        connectMark = true;
        sendMark = true;
        sendRetry = 0;
        receiveMark = true;
        receiveRetry = 0;
    }
    private void Reconnect()
    {
        connectRetry++;
        if (connectRetry == 1)
        {
            socket.Close();
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
    private void ConnectCallback(object sender, EventArgs e)
    {
        connectMark = true;
        connectRetry = 0;
        sendMark = true;
        sendRetry = 0;
        receiveMark = true;
        receiveRetry = 0;
    }
    private void Error(object sender, WebSocketSharp.ErrorEventArgs error)
    {
        GameDebug.LogError(error.Message);
        Reconnect();
    }
    public override void Close()
    {
        base.Close();
        Updater.Instance.StopUpdate(updateId);
        socket.Close();
        socket = null;
        sendMark = false;
    }
    #endregion

    #region 发送
    private void Send()
    {
        if (sendMark && sendQueue.Count > 0)
        {
            sendMark = false;
            var item = sendQueue.Dequeue();
            sendBuffer = Serialize(item.id, item.msg);
            Send(sendBuffer);
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
            int length = 2 + (int)ms.Length;
            byte[] result = new byte[length];
            //消息ID
            byte[] temp = BitConverter.GetBytes(id);
            Buffer.BlockCopy(temp, 0, result, 0, 2);
            //消息内容
            ms.Read(result, 2, length - 2);
            return result;
        }
    }
    private void Send(byte[] bytes)
    {
        try
        {
            socket.SendAsync(sendBuffer, SendCallback);
        }
        catch
        {
            SendCallback(false);
        }
    }
    private void SendCallback(bool result)
    {
        if (connectMark == false)
        {
            return;
        }
        if (result)
        {
            sendMark = true;
            sendRetry = 0;
            return;
        }
        if (sendRetry++ < 1)
        {
            Send(sendBuffer);
            return;
        }
        Reconnect();
    }
    #endregion

    #region 接收
    private void Receive(object sender, MessageEventArgs message)
    {
        if (connectMark == false)
        {
            return;
        }
        if (Deserialize(message.RawData, message.RawData.Length))
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
    #endregion
}