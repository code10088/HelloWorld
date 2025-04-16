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
    private bool sendMark = false;
    private bool receiveMark = false;
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
        sendMark = true;
        receiveMark = true;

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
        socket.Close();
        socket = null;
        kcp.Dispose();
        kcp = null;
        sendPool.Clear();
        sendMark = false;
        receiveMark = false;
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
            Receive();
            Thread.Sleep(GameSetting.updateTimeSliceMS);
        }
    }

    #region 发送
    class KcpSendItem
    {
        public byte[] bytes;
        public int length;
    }
    public void Send(ushort id, IExtensible msg)
    {
        byte[] bytes = Serialize(id, msg);
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
    /// <summary>
    /// kcp.Update中执行
    /// </summary>
    private void Send(byte[] bytes, int length)
    {
        if (length > 0)
        {
            KcpSendItem ksi = new KcpSendItem() { bytes = bytes, length = length };
            sendPool.Enqueue(ksi);
        }
        if (sendMark && sendPool.Count > 0)
        {
            KcpSendItem ksi = sendPool.Dequeue();
            socket.BeginSendTo(ksi.bytes, 0, ksi.length, SocketFlags.None, endPoint, SendCallback, null);
            sendMark = false;
        }
    }
    private void SendCallback(IAsyncResult ar)
    {
        socket.EndSendTo(ar);
        sendMark = true;
    }
    #endregion

    #region 接收
    private void Receive()
    {
        if (!receiveMark) return;
        socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endPoint, ReceiveCallback, null);
        receiveMark = false;
    }
    /// <summary>
    /// 流模式不需要PeekSize
    /// </summary>
    private void ReceiveCallback(IAsyncResult ar)
    {
        int receiveLength = socket.EndReceiveFrom(ar, ref endPoint);
        if (receiveLength > 0) kcp.Input(receiveBuffer.AsSpan(0, receiveLength));
        while ((receiveLength = kcp.Recv(receiveBuffer)) > 0) Parse(receiveLength);
        receiveMark = true;
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