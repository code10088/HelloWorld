using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class TestData : Database
    {
        public List<TestItemData> testItemDatas = new List<TestItemData>()
        {
            new TestItemData(){ id = 1, name="1" },
            new TestItemData(){ id = 2, name="2" },
            new TestItemData(){ id = 3, name="3" },
            new TestItemData(){ id = 4, name="4" },
            new TestItemData(){ id = 5, name="5" },
        };

        public void Clear()
        {
            
        }


        public class TestItemData
        {
            public int id;
            public string name;
        }
    }
}
