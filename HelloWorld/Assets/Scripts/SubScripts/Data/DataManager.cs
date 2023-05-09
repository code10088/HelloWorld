namespace HotAssembly
{
    public class DataManager : Singletion<DataManager>
    {
        private TestData testData;

        public TestData TestData { get => testData ??= new TestData(); }


        public void Clear()
        {
            testData.Clear();
            testData = null;
        }
    }
    public interface Database
    {
        public void Clear();
    }
}