using UnityEngine;
namespace HotAssembly
{
    public partial class UIGuideComponent
    {
        public GameObject obj;
        public UnityEngine.RectTransform maskRectTransform = null;
        public UnityEngine.UI.Image maskImage = null;
        public UIButton maskUIButton = null;
        public GameObject skipBtnObj = null;
        public UIButton skipBtnUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            maskRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
            maskImage = allData[0].exportComponent[1] as UnityEngine.UI.Image;
            maskUIButton = allData[0].exportComponent[2] as UIButton;
            skipBtnObj = allData[1].gameObject;
            skipBtnUIButton = allData[1].exportComponent[1] as UIButton;
        }
    }
}
