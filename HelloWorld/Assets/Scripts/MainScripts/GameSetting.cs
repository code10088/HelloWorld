namespace MainAssembly
{
    public class GameSetting : Singletion<GameSetting>
    {
        public int targetFrame = 60;
        public float updateTimeSliceS = 1.0f / 60;
        public int updateTimeSliceMS = 1000 / 60;
        public int threadLimit = 6;
    }
}