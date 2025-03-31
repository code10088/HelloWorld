using System;

public class TimeUtils
{
	private static float serverTime;

	public static float ServerTime => UnityEngine.Time.time;

	public static string FormatTime(float f)
	{
		var time = TimeSpan.FromSeconds(f);
		if (time.Days > 0) return $"{time.Days}d {time.Hours}h";
		else if (time.Hours > 0) return $"{time.Hours}:{time.Minutes}";
		else if (time.Minutes > 0) return $"{time.Minutes}:{time.Seconds}";
		else return $"{time.Seconds}";
	}
}