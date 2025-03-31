using UnityEngine;
public partial class TestSceneComponent
{
    public GameObject obj;
    public UnityEngine.Transform fireRootTransform = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        fireRootTransform = allData[0].exportComponent[0] as UnityEngine.Transform;
    }
}
