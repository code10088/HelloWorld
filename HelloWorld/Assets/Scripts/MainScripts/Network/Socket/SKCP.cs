using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class SKCP : SBase
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private bool threadMark = true;
    private uint connectId;
    private KcpSend kcpSend;
    private Kcp<KcpSegment> kcp;

    public override void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        base.Init(ip, port, connectId, deserialize, socketevent);
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        this.connectId = connectId;
        kcpSend = new KcpSend(Send);
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
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
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
        if (kcp == null)
        {
            kcp = new Kcp<KcpSegment>(connectId, kcpSend);
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize();
            kcp.SetMtu();
        }
        connectMark = true;
        sendRetry = 0;
        receiveMark = true;
        receiveRetry = 0;
    }
    private void Reconnect()
    {
        connectRetry++;
        if (connectRetry == 1)
        {
            kcp.Dispose();
            kcp = null;
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
    public override void Close()
    {
        base.Close();
        threadMark = false;
        thread?.Join();
        socket.Close();
        socket = null;
        kcp.Dispose();
        kcp = null;
    }
    #endregion

    #region 发送
    class KcpSend : IKcpCallback
    {
        private Action<ArraySegment<byte>> Out;
        public KcpSend(Action<ArraySegment<byte>> _out)
        {
            Out = _out;
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (MemoryMarshal.TryGetArray(buffer.Memory.Slice(0, avalidLength), out ArraySegment<byte> segment)) Out(segment);
        }
    }
    private void Send()
    {
        while (connectMark && sendQueue.Count > 0)
        {
            SendItem item;
            lock (sendQueue) item = sendQueue.Dequeue();
            var bytes = Serialize(item.id, item.msg);
            kcp.Send(bytes);
            bytePool.Return(bytes);
        }
        if (connectMark)
        {
            kcp.Update(DateTime.UtcNow);
        }
    }
    /// <summary>
    /// kcp.Update中执行
    /// </summary>
    private void Send(ArraySegment<byte> bytes)
    {
        if (connectMark) SendTask(bytes);
    }
    private async Task SendTask(ArraySegment<byte> bytes)
    {
        int count = 0;
        try
        {
            count = await socket.SendToAsync(bytes, SocketFlags.None, endPoint);
        }
        catch
        {
            count = -1;
        }
        if (connectMark == false)
        {
            return;
        }
        if (count == bytes.Count)
        {
            sendRetry = 0;
            return;
        }
        if (sendRetry++ < 1)
        {
            SendTask(bytes);
            return;
        }
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
            var result = await socket.ReceiveFromAsync(receiveBuffer, SocketFlags.None, endPoint);
            count = result.ReceivedBytes;
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
            connectRetry = 0;
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
        kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
        while (true)
        {
            int size = kcp.PeekSize();
            if (size <= 0) break;
            var temp = bytePool.Rent(size);
            size = kcp.Recv(temp);
            if (size <= 0)
            {
                bytePool.Return(temp);
                break;
            }
            else
            {
                bool b = Deserialize(temp);
                bytePool.Return(temp);
                if (!b) return false;
            }
        }
        return true;
    }
    #endregion
}