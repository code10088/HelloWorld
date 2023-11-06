using UnityEngine;
namespace HotAssembly
{
    public partial class UICommonBoxComponent
    {
        public GameObject obj;
        public UnityEngine.RectTransform bgRectTransform = null;
        public TMPro.TextMeshProUGUI titleTextMeshProUGUI = null;
        public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
        public UIButton sure1UIButton = null;
        public UIButton sure2UIButton = null;
        public UIButton cancelUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
            titleTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
            contentTextMeshProUGUI = allData[2].exportComponent[0] as TMPro.TextMeshProUGUI;
            sure1UIButton = allData[3].exportComponent[0] as UIButton;
            sure2UIButton = allData[4].exportComponent[0] as UIButton;
            cancelUIButton = allData[5].exportComponent[0] as UIButton;
        }
    }
}
