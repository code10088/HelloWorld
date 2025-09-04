using System;
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
        this.ip = $"{ip}:{port}";
        updateId = Updater.Instance.StartUpdate(Update);
        Connect();
    }
    private void Update(float t)
    {
        Send();
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
            socket = new WebSocket(ip);
            socket.OnOpen += ConnectCallback;
            socket.OnMessage += Receive;
            socket.OnError += Error;
        }
        else
        {
            socket.Close();
        }
        socket.Connect();
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
            socket.Close();
            socket = null;
            Connect();
            return;
        }
        socketevent.Invoke((int)SocketEvent.ConnectError, 0);
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
    private void Error(object sender, ErrorEventArgs error)
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
            var item = sendQueue.Dequeue();
            sendBuffer = Serialize(item.id, item.msg);
            socket.SendAsync(sendBuffer, SendCallback);
            sendMark = false;
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
            socket.SendAsync(sendBuffer, SendCallback);
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
        if (Deserialize(message.RawData))
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