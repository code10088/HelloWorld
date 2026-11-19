using System;

public class ServerTime : Singleton<ServerTime>, SingletonInterface
{
    private double time;
    private int todayKey = 0;

    /// <summary>当前推算的服务器时间（Unix 毫秒）</summary>
    public long Time => (long)time;
    /// <summary>当前推算的服务器时间（Unix 秒）</summary>
    public long TimeSeconds => (long)(time / 1000.0);
    public DateTimeOffset Now => DateTimeOffset.FromUnixTimeMilliseconds((long)time).ToLocalTime();

    public void Init()
    {
        Driver.Instance.StartUpdate(Update, true);
    }
    private void Update(float t)
    {
        time += UnityEngine.Time.unscaledDeltaTime * 1000.0;
        if (todayKey <= 0) return;
        int key = Now.Day;
        if (key == todayKey) return;
        todayKey = key;
        EventManager.Instance.Fire(EventType.CrossDay);
    }

    public void SetTime(long value)
    {
        if (value <= 0) return;
        if (value <= time) return;
        time = value;
        if (todayKey <= 0) todayKey = Now.Day;
    }
}