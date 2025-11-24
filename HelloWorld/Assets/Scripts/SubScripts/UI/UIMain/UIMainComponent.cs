using UnityEngine;
public partial class UIMainComponent
{
    public GameObject obj;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
    }
}
