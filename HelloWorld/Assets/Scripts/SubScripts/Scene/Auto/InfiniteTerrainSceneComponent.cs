using UnityEngine;
public partial class InfiniteTerrainSceneComponent
{
    public GameObject obj;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
    }
}
