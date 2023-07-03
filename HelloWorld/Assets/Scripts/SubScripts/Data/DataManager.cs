namespace HotAssembly
{
    public class DataManager : Singletion<DataManager>
    {
        private TestData testData;
        private HotUpdateResData hotUpdateResData;

        public TestData TestData { get => testData == null ? testData = new TestData() : testData; }
        public HotUpdateResData HotUpdateResData { get => hotUpdateResData == null ? hotUpdateResData = new HotUpdateResData() : hotUpdateResData; }


        public void Clear()
        {
            testData.Clear();
            hotUpdateResData.Clear();
        }
    }
    public interface Database
    {
        void Clear();
    }
}