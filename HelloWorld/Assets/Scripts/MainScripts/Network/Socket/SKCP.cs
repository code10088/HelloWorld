using ProtoBuf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Threading;

public class SKCP
{
    private string ip;
    private ushort port;
    private Socket socket;
    private Thread thread;
    private Kcp<KcpSegment> kcp;
    private KcpReceiveItem kcpReceive;
    private Queue<KcpSendItem> sendPool = new Queue<KcpSendItem>();

    private int connectTimer = -1;
    private bool connectMark = false;

    public void Init(string ip, ushort port)
    {
        this.ip = ip;
        this.port = port;
        kcpReceive = new KcpReceiveItem(Receive);
        Connect();
    }

    #region 连接
    private void Connect()
    {
        IPAddress address = IPAddress.Parse(ip);
        IPEndPoint endPoint = new IPEndPoint(address, port);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);
        socket.BeginConnect(endPoint, ConnectCallback, null);
        if (connectTimer < 0) connectTimer = TimeManager.Instance.StartTimer(10, finish: Reconect);
    }
    private void ConnectCallback(IAsyncResult ar)
    {
        TimeManager.Instance.StopTimer(connectTimer);
        socket.EndConnect(ar);
        if (ar.IsCompleted)
        {
            connectMark = true;
            thread = new Thread(Handle);
            thread.Start();
            kcp = new Kcp<KcpSegment>(port, kcpReceive);
            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);
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
        kcp.Dispose();
        kcp = null;
        sendPool.Clear();
        TimeManager.Instance.StopTimer(connectTimer);
    }
    #endregion

    private void Handle()
    {
        while (true)
        {
            kcp.Update(DateTime.UtcNow);
            if (!connectMark) return;
            while (sendPool.Count > 0)
            {
                if (!connectMark)
                {
                    return;
                }
                lock (sendPool)
                {
                    KcpSendItem nmb = sendPool.Dequeue();
                    kcp.Send(nmb.Serialize());
                }
            }

            int len = 0;
            while ((len = kcp.PeekSize()) > 0)
            {
                if (!connectMark)
                {
                    return;
                }
                var buffer = new byte[len];
                if (kcp.Recv(buffer) >= 0)
                {
                    Receive(buffer);
                }
            }
            if (!connectMark) return;
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    public void Send(ushort id, IExtensible msg)
    {
        KcpSendItem nmb = new KcpSendItem(id, msg);
        lock (sendPool) sendPool.Enqueue(nmb);
    }
    #endregion

    #region 接收
    private void Receive(Memory<byte> buffer)
    {
        kcp.Input(buffer.Span);
    }
    private void Receive(byte[] buffer)
    {
        bool success = SocketManager.Instance.Deserialize(buffer);
        if (!success) Reconect();
    }
    #endregion

    class KcpSendItem
    {
        private ushort id;
        private IExtensible msg;
        public KcpSendItem(ushort id, IExtensible msg)
        {
            this.id = id;
            this.msg = msg;
        }
        public byte[] Serialize()
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
    }
    class KcpReceiveItem : IKcpCallback
    {
        private Action<Memory<byte>> Out;
        public KcpReceiveItem(Action<Memory<byte>> _out)
        {
            Out = _out;
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            using (buffer) Out(buffer.Memory.Slice(0, avalidLength));
        }
    }
}