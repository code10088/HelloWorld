using UnityEngine;
public partial class UIMailComponent
{
    public GameObject obj;
    public UnityEngine.RectTransform bgRectTransform = null;
    public UIButton closeBtnUIButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
        closeBtnUIButton = allData[1].exportComponent[0] as UIButton;
    }
}
