using UnityEngine;
namespace HotAssembly
{
    public partial class UIHotUpdateResComponent
    {
        public GameObject obj;
        public UnityEngine.UI.RawImage bgRawImage = null;
        public UnityEngine.UI.Slider sliderSlider = null;
        public TMPro.TextMeshProUGUI tipsTextMeshProUGUI = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            bgRawImage = allData[0].exportComponent[0] as UnityEngine.UI.RawImage;
            sliderSlider = allData[1].exportComponent[0] as UnityEngine.UI.Slider;
            tipsTextMeshProUGUI = allData[2].exportComponent[0] as TMPro.TextMeshProUGUI;
        }
    }
}
