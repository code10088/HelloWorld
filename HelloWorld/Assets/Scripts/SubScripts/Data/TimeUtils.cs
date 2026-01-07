using System;

public class TimeUtils
{
	private static float serverTime;

	public static float ServerTime => UnityEngine.Time.time;

    /// <summary>
    /// 秒 转 天时分秒
    /// </summary>
    /// <param name="second">秒数</param>
    /// <returns></returns>
    public static string FormatTime(int second)
	{
		var time = TimeSpan.FromSeconds(second);
		if (time.Days > 0) return $"{time.Days}d {time.Hours}h";
		else if (time.Hours > 0) return $"{time.Hours}:{time.Minutes}";
		else if (time.Minutes > 0) return $"{time.Minutes}:{time.Seconds}";
		else return $"{time.Seconds}";
	}
    /// <summary>
    /// 时间戳是多久之前
    /// </summary>
    /// <param name="t">大于 1e12 视为毫秒，否则视为秒</param>
    /// <returns></returns>
    public static string FormatTime(long t)
    {
        var time = t > 1_000_000_000_000L ? DateTimeOffset.FromUnixTimeMilliseconds(t) : DateTimeOffset.FromUnixTimeSeconds(t);
        var now = DateTimeOffset.UtcNow;
        var span = now - time.ToUniversalTime();
        if (span.TotalSeconds < 60) return LanguageManager.Instance.Get(10005);
        if (span.TotalMinutes < 60) return LanguageManager.Instance.Get(10004, span.Minutes);
        if (span.TotalHours < 24) return LanguageManager.Instance.Get(10003, span.Hours);
        return LanguageManager.Instance.Get(10002, span.Days);
    }
}