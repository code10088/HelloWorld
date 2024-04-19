namespace HotAssembly
{
    public interface Database
    {
        void Clear();
    }
    public class DataManager : Singletion<DataManager>
    {
        private TestData testData;
        private HotUpdateResData hotUpdateResData;
        private PlayerData playerData;
        private GuideData guideData;

        public TestData TestData { get => testData == null ? testData = new TestData() : testData; }
        public HotUpdateResData HotUpdateResData { get => hotUpdateResData == null ? hotUpdateResData = new HotUpdateResData() : hotUpdateResData; }
        public PlayerData PlayerData { get => playerData == null ? playerData = new PlayerData() : playerData; }
        public GuideData GuideData { get => guideData == null ? guideData = new GuideData() : guideData; }

        public void Clear()
        {
            testData.Clear();
            hotUpdateResData.Clear();
            playerData.Clear();
            guideData.Clear();
        }
    }
}