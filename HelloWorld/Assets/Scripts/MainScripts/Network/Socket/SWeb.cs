#if UNITY_WEBGL
using NativeWebSocket;
using ProtoBuf;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class SWeb : SBase
{
    private string ip;
    private new WebSocket socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private Task sendTask;

    public override void Init(string ip, ushort port, uint playerId, string token, Func<ushort, Memory<byte>, bool> deserialize, Action<int, int> socketevent)
    {
        this.ip = $"ws://{ip}:{port}/client";
        base.Init(ip, port, playerId, token, deserialize, socketevent);
    }

    #region 连接
    protected override void Connect()
    {
        ConnectAsync();
    }
    protected override async Task<bool> ConnectAsync()
    {
        if (await base.ConnectAsync() == false) return false;
        using (UnityWebRequest request = UnityWebRequest.Head("https://www.baidu.com/"))
        {
            request.timeout = 3;
            var operation = request.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            operation.completed += a => tcs.SetResult(true);
            await tcs.Task;
            if (request.result > UnityWebRequest.Result.Success) return false;
        }
        socket = new WebSocket(ip);
        socket.OnOpen += ConnectCallback;
        socket.OnMessage += Receive;
        socket.OnError += Error;
        socket.Connect();
        return true;
    }
    private void ConnectCallback()
    {
        socketevent.Invoke((int)SocketEvent.Connected, 0);
        connectMark = true;
        connectRetry = 0;
        sendRetry = 0;
        receiveRetry = 0;
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        sendTask = Send(cts.Token);
        heart.Start();
    }
    private void Error(string error)
    {
        GameDebug.LogError(error);
        ConnectAsync();
    }
    public override async Task Close()
    {
        base.Close();
        socket?.Close();
        socket = null;
        signal?.Release();
        signal?.Dispose();
        signal = null;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        await (sendTask ?? Task.CompletedTask);
    }
    #endregion

    #region 发送
    public override void Send(ushort id, IExtensible msg)
    {
        if (connectMark)
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
                await signal.WaitAsync(token);
            }
            catch
            {
                return;
            }
            if (connectMark == false)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                var stream = item.Serialize();
                var buffer = new byte[stream.WPos];
                Buffer.BlockCopy(stream.Buffer, 0, buffer, 0, stream.WPos);
                stream.Dispose();
                await socket.Send(buffer);
                if (connectMark == false)
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
        if (connectMark == false)
        {
            return;
        }
        if (Receive(data, data.Length))
        {
            receiveRetry = 0;
            return;
        }
        if (receiveRetry++ > 0)
        {
            ConnectAsync();
        }
    }
    #endregion
}
#endif