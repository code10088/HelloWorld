using UnityEngine;
namespace HotAssembly
{
    public partial class UICommonTipsComponent
    {
        public GameObject obj;
        public GameObject itemObj = null;
        public UnityExtensions.Tween.TweenPlayer itemTweenPlayer = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            itemObj = allData[0].gameObject;
            itemTweenPlayer = allData[0].exportComponent[1] as UnityExtensions.Tween.TweenPlayer;
        }
    }
    public partial class UICommonTipsItem
    {
        public GameObject obj;
        public GameObject itemObj = null;
        public UnityExtensions.Tween.TweenPlayer itemTweenPlayer = null;
        public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            itemObj = allData[0].gameObject;
            itemTweenPlayer = allData[0].exportComponent[1] as UnityExtensions.Tween.TweenPlayer;
            contentTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
        }
    }
}
