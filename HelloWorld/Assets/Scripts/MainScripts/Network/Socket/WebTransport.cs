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
    private Task sendTask;
    private int receiveRetry = 0;

    public override void Init(string ip, ushort port, uint playerId, string token, IDispatch dispatch)
    {
        this.ip = $"ws://{ip}:{port}/client";
        base.Init(ip, port, playerId, token, dispatch);
    }

    #region 连接
    protected override async Task ConnectTask()
    {
        if (connectRetry++ > 0)
        {
            dispatch.HandleSocketEvent(SocketEvent.ConnectError, 0);
            return;
        }
        dispatch.HandleSocketEvent(SocketEvent.Reconnect, 0);
        await Driver.Instance.RunOnMainThread(() =>
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                dispatch.HandleSocketEvent(SocketEvent.ConnectError, 0);
                return;
            }
            socket = new WebSocket(ip);
            socket.OnOpen += ConnectCallback;
            socket.OnMessage += Receive;
            socket.OnError += Error;
            socket.Connect();
        });
    }
    private void ConnectCallback()
    {
        signal = new SemaphoreSlim(0);
        cts = new CancellationTokenSource();
        State = ConnectState.Connected;
        connectRetry = 0;
        sendTask = Send(cts.Token);
        heart.Start();
        dispatch.HandleSocketEvent(SocketEvent.Connected, 0);
    }
    private void Error(string error)
    {
        GameDebug.LogError(error);
        Connect();
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
