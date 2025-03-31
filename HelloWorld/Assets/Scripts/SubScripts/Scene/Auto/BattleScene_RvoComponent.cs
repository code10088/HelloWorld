using UnityEngine;
public partial class BattleScene_RvoComponent
{
    public GameObject obj;
    public UnityEngine.Transform startTransform = null;
    public GameObject obstacleObj = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        startTransform = allData[0].exportComponent[0] as UnityEngine.Transform;
        obstacleObj = allData[1].gameObject;
    }
}
