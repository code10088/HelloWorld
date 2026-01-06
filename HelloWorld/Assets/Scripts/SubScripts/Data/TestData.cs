using Message;
using System.Collections.Generic;

public class TestData : DataBase
{
    public List<TestItemData> testItemDatas = new List<TestItemData>()
        {
            new TestItemData(){ id = 1, name="1" },
            new TestItemData(){ id = 2, name="2" },
            new TestItemData(){ id = 3, name="3" },
            new TestItemData(){ id = 4, name="4" },
            new TestItemData(){ id = 5, name="5" },
            new TestItemData(){ id = 6, name="6" },
            new TestItemData(){ id = 7, name="7" },
            new TestItemData(){ id = 8, name="8" },
            new TestItemData(){ id = 9, name="9" },
            new TestItemData(){ id = 10, name="10" },
            new TestItemData(){ id = 11, name="11" },
            new TestItemData(){ id = 12, name="12" },
            new TestItemData(){ id = 13, name="13" },
            new TestItemData(){ id = 14, name="14" },
            new TestItemData(){ id = 15, name="15" },
            new TestItemData(){ id = 16, name="16" },
            new TestItemData(){ id = 17, name="17" },
            new TestItemData(){ id = 18, name="18" },
            new TestItemData(){ id = 19, name="19" },
            new TestItemData(){ id = 20, name="20" },
        };

    public void Init()
    {

    }
    public void Clear()
    {

    }

    public class TestItemData
    {
        public int id;
        public string name;
    }
}