using Message;
using System;
using System.Diagnostics;
using UnityEngine;

public class HeartHandle
{
    private int updateId = -1;
    private float timer = 0;
    private int heartInterval = 10;
    private int heartCount = 0;
    private int[] record1 = new int[10];
    private long[] record2 = new long[10];
    private int recordIndex = 0;
    private int delay = 0;
    private CSHeart heart = new CSHeart();

    public int Delay => delay;

    public void Start()
    {
        timer = 0;
        heartCount = 0;
        NetClient.Instance.Register(NetMsgId.Message_SCHeart, SCHeart);
        if (updateId < 0) updateId = Driver.Instance.StartUpdate(Update);
    }
    private void Update(float t)
    {
        timer += Time.unscaledDeltaTime;
        if (timer < heartInterval) return;
        timer = 0;

        if (heartCount++ > 0) NetClient.Instance.Connect();
        else NetClient.Instance.Send(NetMsgId.Message_CSHeart, heart);
    }
    private void SCHeart(IDeserialize msg)
    {
        var heart = (SCHeart)msg;
        ServerTime.Instance.SetTime(heart.time);
    }
    public void RefreshDelay1(ushort id)
    {
        int index = recordIndex++ % record1.Length;
        record1[index] = id;
        record2[index] = Stopwatch.GetTimestamp();
    }
    public void RefreshDelay2(ushort id)
    {
        if (id == NetMsgId.Message_SCHeart) heartCount = 0;
        var index = Array.IndexOf(record1, id - 10000);
        if (index < 0) return;
        record1[index] = -1;
        delay = (int)((Stopwatch.GetTimestamp() - record2[index]) * 1000L / Stopwatch.Frequency);
        heartInterval = delay > 10000 ? 3 : 10;
        EventManager.Instance.Fire(EventType.RefreshDelay, delay);
    }
    public void Stop()
    {
        Driver.Instance.Remove(updateId);
        updateId = -1;
        timer = 0;
        heartCount = 0;
        Array.Clear(record1, 0, record1.Length);
        Array.Clear(record2, 0, record2.Length);
        recordIndex = 0;
        delay = 0;
        NetClient.Instance.Unregister(NetMsgId.Message_SCHeart);
    }
}