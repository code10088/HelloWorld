using UnityEngine;
namespace HotAssembly
{
    public partial class UITestComponent
    {
        public GameObject obj;
        public UnityEngine.UI.Button buttonButton = null;
        public HotAssembly.LoopVerticalScrollRect loopLoopVerticalScrollRect = null;
        public GameObject itemObj = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            buttonButton = allData[0].exportComponent[0] as UnityEngine.UI.Button;
            loopLoopVerticalScrollRect = allData[1].exportComponent[0] as HotAssembly.LoopVerticalScrollRect;
            itemObj = allData[2].gameObject;
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
