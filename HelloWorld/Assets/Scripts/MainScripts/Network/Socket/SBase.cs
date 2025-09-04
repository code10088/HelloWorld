using ProtoBuf;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum SocketEvent
{
    NetworkChange,
    ConnectError,
    RefreshDelay,
}
public class SBase
{
    public class SendItem
    {
        public ushort id;
        public IExtensible msg;
    }
    [ProtoContract]
    public class CS_Heart : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
    }

    
    private Func<ushort, Memory<byte>, bool> deserialize;
    protected Action<int, int> socketevent;
    protected ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;

    //连接
    private NetworkReachability reach;
    protected bool connectMark = false;
    protected int connectRetry = 0;

    //发送
    protected Queue<SendItem> sendQueue = new Queue<SendItem>(10);
    protected int sendRetry = 0;

    //接收
    protected bool receiveMark = false;
    protected int receiveRetry = 0;
    protected byte[] receiveBuffer = new byte[1024];

    //心跳
    private bool heartMark = true;
    private float heartTimer = 0;
    protected int heartInterval = 10000;
    private ushort[] record1 = new ushort[10];
    private long[] record2 = new long[10];
    private int recordIndex = 0;
    private CS_Heart heart = new CS_Heart();

    public virtual void Init(string ip, ushort port, uint connectId, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.deserialize = deserialize;
        this.socketevent = socketevent;
    }

    #region 连接
    public virtual void Connect()
    {

    }
    protected bool CheckNetworkNotReachable()
    {
        var temp = Application.internetReachability;
        if (temp != reach) socketevent.Invoke((int)SocketEvent.NetworkChange, 0);
        reach = temp;
        return temp == NetworkReachability.NotReachable;
    }
    public virtual void Close()
    {
        connectMark = false;
        connectRetry = 0;
        sendQueue.Clear();
        sendRetry = 0;
        receiveMark = false;
        receiveRetry = 0;
        heartTimer = 0;
        heartInterval = 10000;
        recordIndex = 0;
    }
    #endregion

    #region 发送
    public virtual void Send(ushort id, IExtensible msg)
    {
        RefreshDelay1(id);
        SendItem item = new SendItem() { id = id, msg = msg };
        lock (sendQueue) sendQueue.Enqueue(item);
    }
    protected byte[] Serialize(ushort id, IExtensible msg)
    {
        return Serialize(id, msg, false, out int length);
    }
    protected byte[] Serialize(ushort id, IExtensible msg, out int length)
    {
        return Serialize(id, msg, true, out length);
    }
    /// <summary>
    /// Array.Copy < Buffer.BlockCopy < Buffer.MemoryCopy
    /// </summary>
    private byte[] Serialize(ushort id, IExtensible msg, bool rent, out int length)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            Serializer.Serialize(ms, msg);
            length = 6 + (int)ms.Length;
            byte[] result = rent ? bytePool.Rent(length) : new byte[length];
            //消息长度
            byte[] temp = BitConverter.GetBytes(length - 4);
            Buffer.BlockCopy(temp, 0, result, 0, 4);
            //消息ID
            temp = BitConverter.GetBytes(id);
            Buffer.BlockCopy(temp, 0, result, 4, 2);
            //消息内容
            ms.Read(result, 6, length - 6);
            return result;
        }
    }
    #endregion

    #region 接收
    protected bool Deserialize(byte[] bytes, int length = 0)
    {
        if (length == 0) length = bytes.Length;
        var temp = bytes.AsMemory(0, length);
        var id = BitConverter.ToUInt16(temp.Span.Slice(0, 2));
        var b = deserialize(id, temp.Slice(2));
        RefreshDelay2(id);
        return b;
    }
    #endregion

    #region 心跳
    public void SetHeartState(bool open)
    {
        heartMark = open;
    }
    protected void UpdateHeart(float t)
    {
        if (!heartMark || !connectMark) return;
        heartTimer += t;
        if (heartTimer < heartInterval) return;
        Send(0, heart);
        heartTimer = 0;
    }
    private void RefreshDelay1(ushort id)
    {
        int index = recordIndex++ % record1.Length;
        record1[index] = id;
        record2[index] = DateTime.UtcNow.Ticks;
    }
    private void RefreshDelay2(ushort id)
    {
        var index = Array.IndexOf(record1, id - 10000);
        if (index < 0) return;
        record1[index] = 0;
        var delay = (DateTime.UtcNow.Ticks - record2[index]) / 10000;
        heartTimer = 0;
        heartInterval = delay > 100 ? 2000 : 10000;
        socketevent.Invoke((int)SocketEvent.RefreshDelay, (int)delay);
    }
    #endregion
}