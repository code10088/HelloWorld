using UnityEngine;
namespace MainAssembly
{
    public partial class UIHotUpdateCodeComponent
    {
        public GameObject obj;
        public UnityEngine.UI.Slider sliderSlider = null;
        public TMPro.TextMeshProUGUI tipsTextMeshProUGUI = null;
        public GameObject messageBoxObj = null;
        public TMPro.TextMeshProUGUI titleTextMeshProUGUI = null;
        public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
        public UnityEngine.UI.Button sureButton = null;
        public UnityEngine.UI.Button cancelButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            sliderSlider = allData[0].exportComponent[0] as UnityEngine.UI.Slider;
            tipsTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
            messageBoxObj = allData[2].gameObject;
            titleTextMeshProUGUI = allData[3].exportComponent[0] as TMPro.TextMeshProUGUI;
            contentTextMeshProUGUI = allData[4].exportComponent[0] as TMPro.TextMeshProUGUI;
            sureButton = allData[5].exportComponent[0] as UnityEngine.UI.Button;
            cancelButton = allData[6].exportComponent[0] as UnityEngine.UI.Button;
        }
    }
}
