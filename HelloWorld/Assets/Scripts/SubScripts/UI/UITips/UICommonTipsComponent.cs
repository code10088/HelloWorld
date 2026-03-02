using UnityEngine;
public partial class UICommonTipsComponent
{
    public GameObject obj;
    public GameObject itemObj = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        itemObj = allData[0].gameObject;
    }
}
public partial class UICommonTipsItem
{
    public GameObject obj;
    public GameObject itemObj = null;
    public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        itemObj = allData[0].gameObject;
        contentTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
    }
}
