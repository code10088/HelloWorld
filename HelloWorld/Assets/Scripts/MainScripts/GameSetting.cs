public static class GameSetting
{
    public static int targetFrame = 60;
    public static float updateTimeSliceS = 1.0f / 60;
    public static int updateTimeSliceMS = 1000 / 60;
    public static int gcTimeIntervalS = 600;//GC���
    public static int threadLimit = 6;
    public static int httpLimit = 3;//С��threadLimit
    public static int writeLimit = 3;//С��threadLimit
}