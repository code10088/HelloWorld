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
        private GuideData guideData;

        public TestData TestData { get => testData == null ? testData = new TestData() : testData; }
        public HotUpdateResData HotUpdateResData { get => hotUpdateResData == null ? hotUpdateResData = new HotUpdateResData() : hotUpdateResData; }
        public GuideData GuideData { get => guideData == null ? guideData = new GuideData() : guideData; }

        public void Clear()
        {
            testData.Clear();
            hotUpdateResData.Clear();
            guideData.Clear();
        }
    }
}