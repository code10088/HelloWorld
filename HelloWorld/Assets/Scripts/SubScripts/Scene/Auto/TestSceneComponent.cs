using UnityEngine;
namespace HotAssembly
{
    public partial class TestSceneComponent
    {
        public GameObject obj;
        public GameObject cubeObj = null;
        public UnityEngine.Transform fireRootTransform = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            cubeObj = allData[0].gameObject;
            fireRootTransform = allData[1].exportComponent[0] as UnityEngine.Transform;
        }
    }
}
