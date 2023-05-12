namespace HotAssembly
{
    public class DataManager : Singletion<DataManager>
    {
        private TestData testData;
        private HotUpdateResData hotUpdateResData;

        public TestData TestData { get => testData ??= new TestData(); }
        public HotUpdateResData HotUpdateResData { get => hotUpdateResData ??= new HotUpdateResData(); }


        public void Clear()
        {
            testData.Clear();
            hotUpdateResData.Clear();
        }
    }
    public interface Database
    {
        public void Clear();
    }
}