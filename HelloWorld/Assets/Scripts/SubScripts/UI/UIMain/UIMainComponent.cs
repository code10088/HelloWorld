using UnityEngine;
public partial class UIMainComponent
{
    public GameObject obj;
    public UIButton uIMailBtnUIButton = null;
    public UIButton uITestBtnUIButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        uIMailBtnUIButton = allData[0].exportComponent[0] as UIButton;
        uITestBtnUIButton = allData[1].exportComponent[0] as UIButton;
    }
}
