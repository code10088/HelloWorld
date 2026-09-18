using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class HeartHandle
{
    private CancellationTokenSource cts;
    private Task heartTask;
    private int heartInterval = 10000;
    private int heartCount = 0;
    private int[] record1 = new int[10];
    private long[] record2 = new long[10];
    private int recordIndex = 0;
    private int delay = 0;
    private Action connect;
    private Action<ushort, ISerialize> send;
    private CS_Heart heart = new CS_Heart();

    public int Delay => delay;

    public HeartHandle(Action connect, Action<ushort, ISerialize> send)
    {
        this.connect = connect;
        this.send = send;
    }
    public void Start()
    {
        cts = new CancellationTokenSource();
        heartTask = UpdateHeart(cts.Token);
    }
    private async Task UpdateHeart(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await Task.Delay(heartInterval, token).ConfigureAwait(false);
                if (heartCount++ > 0) connect();
                else send(NetMsgId.CSHeart, heart);
            }
        }
        catch
        {
        }
    }
    public void RefreshDelay1(ushort id)
    {
        int index = recordIndex++ % record1.Length;
        record1[index] = id;
        record2[index] = Stopwatch.GetTimestamp();
    }
    public void RefreshDelay2(ushort id)
    {
        if (id == NetMsgId.SCHeart) heartCount = 0;
        var index = Array.IndexOf(record1, id - 10000);
        if (index < 0) return;
        record1[index] = -1;
        delay = (int)((Stopwatch.GetTimestamp() - record2[index]) * 1000L / Stopwatch.Frequency);
        heartInterval = delay > 10000 ? 3000 : 10000;
    }
    public async Task Dispose()
    {
        cts?.Cancel();
        await (heartTask ?? Task.CompletedTask);
        cts?.Dispose();
        cts = null;
        heartTask = null;
        heartCount = 0;
        recordIndex = 0;
        delay = 0;
    }
}
