#if UNITY_WEBGL
using NativeWebSocket;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebTransport : TransportBase
{
    private string ip;
    private WebSocket socket;
    private SemaphoreSlim signal;
    private CancellationTokenSource cts;
    private TaskCompletionSource<bool> tcs;
    private Task sendTask;
    private int receiveRetry = 0;

    public override void Init(string ip, ushort port, uint playerId, string token, IDispatch dispatch)
    {
        this.ip = $"ws://{ip}:{port}/client";
        base.Init(ip, port, playerId, token, dispatch);
    }

    #region 连接
    protected override async Task<bool> TestTask()
    {
        var result = false;
        await Driver.Instance.RunOnMainThread(() => result = Application.internetReachability > NetworkReachability.NotReachable);
        return result;
    }
    protected override async Task<bool> ConnectTask()
    {
        socket = new WebSocket(ip);
        socket.OnOpen += OnOpen;
        socket.OnMessage += Receive;
        socket.OnClose += OnClose;
        socket.OnError += OnError;
        tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Driver.Instance.RunOnMainThread(() => socket.Connect());
        await tcs.Task.ConfigureAwait(false);
        return tcs.Task.Result;
    }
    private void OnOpen()
    {
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        sendTask = Send(cts.Token);
        tcs.TrySetResult(true);
    }
    private void OnClose(WebSocketCloseCode closeCode)
    {
        GameDebug.LogError(closeCode);
        tcs.TrySetResult(false);
    }
    private void OnError(string error)
    {
        GameDebug.LogError(error);
        tcs.TrySetResult(false);
    }
    protected override async Task CloseTask()
    {
        cts?.Cancel();
        signal?.Release();
        await Driver.Instance.RunOnMainThread(() => socket?.Close());
        await base.CloseTask();
        await (sendTask ?? Task.CompletedTask);
        cts?.Dispose();
        signal?.Dispose();
        cts = null;
        signal = null;
        sendTask = null;
        socket = null;
    }
    #endregion

    #region 发送
    public override void Send(ushort id, ISerialize msg)
    {
        if (State == ConnectState.Connected)
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
            if (State != ConnectState.Connected)
            {
                return;
            }
            while (sendQueue.TryDequeue(out var item))
            {
                item.Serialize(sendBuffer);
                var bytes = sendBuffer.Span.ToArray();
                await socket.Send(bytes).ConfigureAwait(false);
                if (State != ConnectState.Connected)
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
        if (State != ConnectState.Connected)
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
