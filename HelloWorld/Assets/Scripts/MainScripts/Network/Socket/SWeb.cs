using System;
using System.Threading.Tasks;
using WebSocketSharp;

public class SWeb : SBase
{
    private string ip;
    private new WebSocket socket;
    private int updateId = -1;
    private bool sendMark = false;
    private byte[] sendBuffer;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.ip = $"ws://{ip}:{port}/client";
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override async Task<bool> Connect()
    {
        if (await base.Connect() == false) return false;
        socket = new WebSocket(ip);
        socket.OnOpen += ConnectCallback;
        socket.OnMessage += Receive;
        socket.OnError += Error;
        socket.ConnectAsync();//连接失败不会调用ConnectCallback
        updateId = Driver.Instance.StartUpdate(Send);
        connectMark = true;
        sendMark = true;
        sendRetry = 0;
        receiveRetry = 0;
        heart.Start();
        return true;
    }
    private void ConnectCallback(object sender, EventArgs e)
    {
        socketevent.Invoke((int)SocketEvent.Connected, 0);
        connectMark = true;
        connectRetry = 0;
        sendMark = true;
        sendRetry = 0;
        receiveRetry = 0;
    }
    private void Error(object sender, ErrorEventArgs error)
    {
        GameDebug.LogError(error.Message);
        Connect();
    }
    public override async Task Close()
    {
        base.Close();
        Driver.Instance.Remove(updateId);
        socket?.Close();
        socket = null;
        sendMark = false;
        sendBuffer?.Return();
        sendBuffer = null;
    }
    #endregion

    #region 发送
    private void Send(float t)
    {
        if (sendMark && sendQueue.TryDequeue(out var item))
        {
            sendMark = false;
            var wb = serialize.Serialize(item.Id, item.Msg);
            sendBuffer = BufferPool.Rent(wb.Pos);
            Buffer.BlockCopy(wb.Buffer, 0, sendBuffer, 0, wb.Pos);
            wb.Dispose();
            Send(sendBuffer);
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
            sendBuffer?.Return();
            sendBuffer = null;
            sendMark = true;
            sendRetry = 0;
            return;
        }
        if (sendRetry++ > 0)
        {
            Connect();
        }
        else
        {
            Send(sendBuffer);
        }
    }
    #endregion

    #region 接收
    private void Receive(object sender, MessageEventArgs message)
    {
        if (connectMark == false)
        {
            return;
        }
        if (Receive(message.RawData, message.RawData.Length))
        {
            receiveRetry = 0;
            return;
        }
        if (receiveRetry++ > 0)
        {
            Connect();
        }
    }
    #endregion
}