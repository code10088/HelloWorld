using UnityEngine;
namespace HotAssembly
{
    public partial class UISubTestComponent
    {
        public GameObject obj;
        public UIButton buttonUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            buttonUIButton = allData[0].exportComponent[0] as UIButton;
        }
    }
}
