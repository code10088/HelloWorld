using UnityEngine;
namespace HotAssembly
{
    public partial class UISceneLoadingComponent
    {
        public GameObject obj;
        public UnityEngine.RectTransform bgRectTransform = null;
        public UnityEngine.UI.RawImage bgRawImage = null;
        public UnityEngine.UI.Slider sliderSlider = null;
        public TMPro.TextMeshProUGUI tipsTextMeshProUGUI = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
            bgRawImage = allData[0].exportComponent[1] as UnityEngine.UI.RawImage;
            sliderSlider = allData[1].exportComponent[0] as UnityEngine.UI.Slider;
            tipsTextMeshProUGUI = allData[2].exportComponent[0] as TMPro.TextMeshProUGUI;
        }
    }
}
