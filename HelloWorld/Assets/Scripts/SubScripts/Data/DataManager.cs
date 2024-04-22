namespace HotAssembly
{
    public interface DataBase
    {
        void Clear();
    }
    public class DataManager : Singletion<DataManager>
    {
        private TestData testData;
        private HotUpdateResData hotUpdateResData;
        private PlayerData playerData;
        private GuideData guideData;
        private ActivityData activityData;

        public TestData TestData { get => testData == null ? testData = new TestData() : testData; }
        public HotUpdateResData HotUpdateResData { get => hotUpdateResData == null ? hotUpdateResData = new HotUpdateResData() : hotUpdateResData; }
        public PlayerData PlayerData { get => playerData == null ? playerData = new PlayerData() : playerData; }
        public GuideData GuideData { get => guideData == null ? guideData = new GuideData() : guideData; }
        public ActivityData ActivityData { get => activityData == null ? activityData = new ActivityData() : activityData; }

        public void Clear()
        {
            testData.Clear();
            hotUpdateResData.Clear();
            playerData.Clear();
            guideData.Clear();
            activityData.Clear();
        }
    }
}