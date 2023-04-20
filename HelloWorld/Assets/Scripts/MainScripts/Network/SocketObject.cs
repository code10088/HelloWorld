using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MainAssembly
{
    public class SocketObject
    {
        private string ip;
        private ushort port;
        private Socket socket;
        private Thread thread;
        private Queue<NetMessageBase> sendPool = new Queue<NetMessageBase>();
        private Queue<byte[]> receivePool = new Queue<byte[]>();

        private int connectTimer;
        private int receiveTimer;
        private int sendFailCount = 0;
        private int receiveFailCount = 0;
        private bool receiveMark = false;
        private bool disconnectMark = false;
        private byte[] receiveBuffer = new byte[1024];
        private byte[] headBuffer = new byte[6];
        private byte[] bodyBuffer;
        private int headPos = 0;
        private int bodyPos = 0;
        private int headLength = 6;
        private int bodyLength = 0;

        public void Init(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
            Connect();
        }

        #region 连接
        private void Connect()
        {
            IPAddress address = IPAddress.Parse(ip);
            IPEndPoint endPoint = new IPEndPoint(address, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(endPoint, ConnectCallback, null);
            connectTimer = TimeManager.Instance.StartTimer(10, finish: ConnectFail);
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            TimeManager.Instance.StopTimer(connectTimer);
            socket.EndConnect(ar);
            if (ar.IsCompleted)
            {
                receiveMark = true;
                thread = new Thread(Handle);
                thread.Start();
            }
            else
            {
                ConnectFail();
            }
        }
        private void ConnectFail()
        {
            thread?.Abort();
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
            socket.Disconnect(false);
            socket.Close();
            socket = null;

            thread?.Abort();
            sendPool.Clear();
            receivePool.Clear();
            TimeManager.Instance.StopTimer(connectTimer);
            TimeManager.Instance.StopTimer(receiveTimer);
            sendFailCount = 0;
            receiveFailCount = 0;
            receiveMark = false;
            disconnectMark = true;
            bodyBuffer = null;
            headPos = 0;
            bodyPos = 0;
            bodyLength = 0;
        }
        #endregion

        private void Handle()
        {
            while (true)
            {
                while (sendPool.Count > 0)
                {
                    if (disconnectMark)
                    {
                        break;
                    }
                    lock (sendPool)
                    {
                        NetMessageBase nmb = sendPool.Dequeue();
                        nmb.Send(this);
                    }
                }
                while (receiveMark)
                {
                    BeginReceive();
                }
                while (receivePool.Count > 0)
                {
                    lock (receivePool)
                    {
                        byte[] bytes = receivePool.Dequeue();
                        //TODO protobuf Deserialize

                    }
                }
                Thread.Sleep(GameSetting.Instance.updateTimeSliceMS);
            }
        }

        #region 发送
        public void BeginSend(byte[] bytes, AsyncCallback callback)
        {
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, callback, null);
        }
        public int EndSend(IAsyncResult ar)
        {
            return socket.EndSend(ar);
        }
        public void Send(bool result, NetMessageBase nmb)
        {
            if (result)
            {
                sendFailCount = 0;
            }
            else if (sendFailCount < 3)
            {
                sendFailCount++;
                Send(nmb);
            }
            else
            {
                Reconect();
            }
        }
        public void Send(NetMessageBase nmb)
        {
            lock (sendPool) sendPool.Enqueue(nmb);
        }
        #endregion

        #region 接收
        private void BeginReceive()
        {
            receiveMark = false;
            socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, null);
            receiveTimer = TimeManager.Instance.StartTimer(10, finish: () => ReceiveCallback(0));
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            TimeManager.Instance.StopTimer(receiveTimer);
            int receiveLength = socket.EndReceive(ar);
            ReceiveCallback(receiveLength);
        }
        private void ReceiveCallback(int receiveLength)
        {
            if (receiveLength > 0)
            {
                Parse(receiveLength);
                receiveFailCount = 0;
                receiveMark = true;
            }
            else if (receiveFailCount < 3)
            {
                receiveFailCount++;
                receiveMark = true;
            }
            else
            {
                Reconect();
            }
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
                        bodyBuffer = new byte[bodyLength];
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
                        lock (receivePool) receivePool.Enqueue(bodyBuffer);
                        headPos = 0;
                        bodyPos = 0;
                    }
                }
            }
        }
        #endregion
    }
}