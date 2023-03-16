namespace MainAssembly
{
    public class GameSetting : Singletion<GameSetting>
    {
        public int targetFrame = 60;
        public float updateTimeSlice = 1.0f / 60;
    }
}