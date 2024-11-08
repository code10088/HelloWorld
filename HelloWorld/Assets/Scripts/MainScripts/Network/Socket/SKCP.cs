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
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private Kcp<KcpSegment> kcp;
    private KcpSend kcpSend;
    private uint connectId;
    private Queue<KcpSendItem> sendPool = new Queue<KcpSendItem>();

    private bool connectMark = false;
    private byte[] receiveBuffer = new byte[1024];

    public void Init(string ip, ushort port, uint connectId)
    {
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        kcpSend = new KcpSend(Send);
        this.connectId = connectId;
        Connect();
    }

    #region 连接
    private void Connect()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endPoint);

        kcp = new Kcp<KcpSegment>(connectId, kcpSend);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();

        thread = new Thread(Handle);
        thread.Start();
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
    }
    #endregion

    private void Handle()
    {
        while (true)
        {
            kcp.Update(DateTime.UtcNow);
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
        KcpSendItem nmb = new KcpSendItem(id, msg);
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
                KcpSendItem nmb = sendPool.Dequeue();
                nmb.Send(kcp);
            }
        }
    }
    private void Send(byte[] buffer)
    {
        socket.SendTo(buffer, endPoint);
    }
    #endregion

    #region 接收
    private void Receive()
    {
        int receiveLength = socket.ReceiveFrom(receiveBuffer, ref endPoint);
        kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
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
                bool success = Deserialize(buffer);
                if (!success) return;
            }
        }
    }
    private bool Deserialize(byte[] buffer)
    {
        bool success = SocketManager.Instance.Deserialize(buffer);
        if (!success) Reconect();
        return success;
    }
    #endregion

    class KcpSend : IKcpCallback
    {
        private Action<byte[]> Out;
        public KcpSend(Action<byte[]> _out)
        {
            Out = _out;
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            using (buffer) Out(buffer.Memory.Slice(0, avalidLength).ToArray());
        }
    }
    class KcpSendItem
    {
        private ushort id;
        private IExtensible msg;
        public KcpSendItem(ushort id, IExtensible msg)
        {
            this.id = id;
            this.msg = msg;
        }
        public void Send(Kcp<KcpSegment> kcp)
        {
            kcp.Send(Serialize());
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
    }
}