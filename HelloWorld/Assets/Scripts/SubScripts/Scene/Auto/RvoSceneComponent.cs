using UnityEngine;
namespace HotAssembly
{
    public partial class RvoSceneComponent
    {
        public GameObject obj;
        public UnityEngine.Transform tankTransform = null;
        public GameObject obstacleObj = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            tankTransform = allData[0].exportComponent[0] as UnityEngine.Transform;
            obstacleObj = allData[1].gameObject;
        }
    }
}
