using UnityEngine;
namespace HotAssembly
{
    public partial class UIMainComponent
    {
        public GameObject obj;
        public UIButton openUITestBtnUIButton = null;
        public UIButton openUISettingUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            openUITestBtnUIButton = allData[0].exportComponent[0] as UIButton;
            openUISettingUIButton = allData[1].exportComponent[0] as UIButton;
        }
    }
}
