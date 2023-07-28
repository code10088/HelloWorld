using UnityEngine;
namespace HotAssembly
{
    public partial class UITestComponent
    {
        public GameObject obj;
        public UnityEngine.UI.Button closeBtnButton = null;
        public UnityEngine.UI.LoopVerticalScrollRect loopLoopVerticalScrollRect = null;
        public GameObject itemObj = null;
        public GameObject subRootObj = null;
        public UnityEngine.UI.Button openSubBtnButton = null;
        public UnityEngine.UI.Button openMsgBtnButton = null;
        public UnityEngine.UI.Button openSDKBtnButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            closeBtnButton = allData[0].exportComponent[0] as UnityEngine.UI.Button;
            loopLoopVerticalScrollRect = allData[1].exportComponent[0] as UnityEngine.UI.LoopVerticalScrollRect;
            itemObj = allData[2].gameObject;
            subRootObj = allData[4].gameObject;
            openSubBtnButton = allData[5].exportComponent[0] as UnityEngine.UI.Button;
            openMsgBtnButton = allData[6].exportComponent[0] as UnityEngine.UI.Button;
            openSDKBtnButton = allData[7].exportComponent[0] as UnityEngine.UI.Button;
        }
    }
    public partial class UITestItem
    {
        public GameObject obj;
        public TMPro.TextMeshProUGUI itemTextTextMeshProUGUI = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            itemTextTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
        }
    }
}
