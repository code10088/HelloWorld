using UnityEngine;
namespace HotAssembly
{
    public partial class UITestComponent
    {
        public GameObject obj;
        public UIButton closeBtnUIButton = null;
        public SuperScrollView.LoopListView2 loopLoopListView2 = null;
        public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
        public GameObject subRootObj = null;
        public UIButton openSubBtnUIButton = null;
        public UIButton openMsgBtnUIButton = null;
        public UIButton openSDKBtnUIButton = null;
        public UIButton loadSpriteUIButton = null;
        public UnityEngine.UI.Image imageImage = null;
        public UIButton poolEnqueueUIButton = null;
        public UIButton poolDequeueUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            closeBtnUIButton = allData[0].exportComponent[0] as UIButton;
            loopLoopListView2 = allData[1].exportComponent[0] as SuperScrollView.LoopListView2;
            itemLoopListViewItem2 = allData[2].exportComponent[0] as SuperScrollView.LoopListViewItem2;
            subRootObj = allData[4].gameObject;
            openSubBtnUIButton = allData[5].exportComponent[0] as UIButton;
            openMsgBtnUIButton = allData[6].exportComponent[0] as UIButton;
            openSDKBtnUIButton = allData[7].exportComponent[0] as UIButton;
            loadSpriteUIButton = allData[8].exportComponent[0] as UIButton;
            imageImage = allData[9].exportComponent[0] as UnityEngine.UI.Image;
            poolEnqueueUIButton = allData[10].exportComponent[0] as UIButton;
            poolDequeueUIButton = allData[11].exportComponent[0] as UIButton;
        }
    }
    public partial class UITestItem : LoopItemData
    {
        public GameObject obj;
        public SuperScrollView.LoopListViewItem2 itemLoopListViewItem2 = null;
        public TMPro.TextMeshProUGUI itemTextTextMeshProUGUI = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            itemLoopListViewItem2 = allData[0].exportComponent[0] as SuperScrollView.LoopListViewItem2;
            itemTextTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
        }
    }
}
