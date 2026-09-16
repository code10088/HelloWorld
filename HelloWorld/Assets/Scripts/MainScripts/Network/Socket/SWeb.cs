#if UNITY_WEBGL
using NativeWebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SWeb : SBase
{
    private string ip;
    private new WebSocket socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;
    private int receiveRetry = 0;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, UnsafeByteBuffer, bool> deserialize, Action<int, int> socketevent)
    {
        this.ip = $"ws://{ip}:{port}/client";
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override void Connect()
    {
        ConnectAsync();
    }
    private async Task ConnectAsync()
    {
        await Close();
        if (connectRetry++ > 0)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        socketevent.Invoke((int)SocketEvent.Reconect, 0);
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            socketevent.Invoke((int)SocketEvent.ConnectError, 0);
            return;
        }
        socket = new WebSocket(ip);
        socket.OnOpen += ConnectCallback;
        socket.OnMessage += Receive;
        socket.OnError += Error;
        await socket.Connect();
    }
    private void ConnectCallback()
    {
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        Connected = true;
        connectRetry = 0;
        sendTask = Send(cts.Token);
        heart.Start();
        socketevent.Invoke((int)SocketEvent.Connected, 0);
    }
    private void Error(string error)
    {
        GameDebug.LogError(error);
        Connect();
    }
    public override async Task Close()
    {
        cts?.Cancel();
        signal?.Release();
        socket?.Close();
        await base.Close();
        await (sendTask ?? Task.CompletedTask);
        cts?.Dispose();
        signal?.Dispose();
        cts = null;
        signal = null;
        socket = null;
        sendTask = null;
    }
    #endregion

    #region 发送
    public override void Send(ushort id, ISerialize msg)
    {
        if (Connected)
        {
            base.Send(id, msg);
            signal?.Release();
        }
    }
    private async Task Send(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await signal.WaitAsync(token).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
            if (Connected == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                item.Serialize(sendBuffer);
                var bytes = sendBuffer.Span.ToArray();
                await socket.Send(bytes).ConfigureAwait(false);
                if (Connected == false)
                {
                    return;
                }
            }
        }
    }
    #endregion

    #region 接收
    private void Receive(byte[] data)
    {
        if (Connected == false)
        {
            return;
        }
        receiveBuffer.Clear();
        receiveBuffer.WriteSpan(data);
        if (Receive(receiveBuffer))
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
#endif
