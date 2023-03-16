namespace MainAssembly
{
    public class GameData : Singletion<GameData>
    {
        public int targetFrame = 60;
        public float updateTimeSlice = 1.0f / 60;
    }
}