using ProtoBuf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Runtime.InteropServices;
using System.Threading;

public class SKCP : SInterface
{
    private EndPoint endPoint;
    private Socket socket;
    private Thread thread;
    private Kcp<KcpSegment> kcp;
    private KcpSend kcpSend;
    private uint connectId;
    private Func<byte[], bool> deserialize;
    private Queue<KcpSendItem> sendPool = new Queue<KcpSendItem>();

    private bool connectMark = false;
    private byte[] receiveBuffer = new byte[1024];
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer;
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public void Init(string ip, ushort port, uint connectId, Func<byte[], bool> deserialize)
    {
        IPAddress address = IPAddress.Parse(ip);
        endPoint = new IPEndPoint(address, port);
        kcpSend = new KcpSend(Send);
        this.connectId = connectId;
        this.deserialize = deserialize;
        Connect();
    }

    #region 连接
    /// <summary>
    /// UDP无连接协议，BeginConnect仅记录目标地址和端口
    /// </summary>
    private void Connect()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        connectMark = true;

        kcp = new Kcp<KcpSegment>(connectId, kcpSend);
        kcp.NoDelay(1, 10, 2, 1);
        kcp.WndSize();
        kcp.SetMtu();

        thread = new Thread(Handle);
        thread.Start();
    }
    public void Disconnect()
    {
        connectMark = false;//会影响线程中断
        thread?.Join();
        socket.Close();
        socket = null;
        kcp.Dispose();
        kcp = null;
        sendPool.Clear();
        bodyBuffer = null;
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
    #endregion

    private void Handle()
    {
        while (connectMark)
        {
            kcp.Update(DateTime.UtcNow);
            Send();
            Receive();
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    class KcpSendItem
    {
        public ushort id;
        public IExtensible msg;
    }
    public void Send(ushort id, IExtensible msg)
    {
        KcpSendItem ksi = new KcpSendItem() { id = id, msg = msg };
        lock (sendPool) sendPool.Enqueue(ksi);
    }
    private void Send()
    {
        if (sendPool.Count == 0) return;
        KcpSendItem ksi;
        lock (sendPool) ksi = sendPool.Dequeue();
        byte[] bytes = Serialize(ksi.id, ksi.msg);
        kcp.Send(bytes);
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
    private void Send(byte[] buffer, int length)
    {
        if (length > 0) socket.SendTo(buffer, length, SocketFlags.None, endPoint);
    }
    #endregion

    #region 接收
    /// <summary>
    /// 流模式不需要PeekSize
    /// </summary>
    private void Receive()
    {
        int receiveLength = socket.ReceiveFrom(receiveBuffer, ref endPoint);
        if (receiveLength <= 0) return;
        kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
        while ((receiveLength = kcp.Recv(receiveBuffer)) > 0) Parse(receiveLength);
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

    class KcpSend : IKcpCallback
    {
        private Action<byte[], int> Out;
        public KcpSend(Action<byte[], int> _out)
        {
            Out = _out;
        }
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (MemoryMarshal.TryGetArray(buffer.Memory, out ArraySegment<byte> segment)) Out(segment.Array, avalidLength);
        }
    }
}