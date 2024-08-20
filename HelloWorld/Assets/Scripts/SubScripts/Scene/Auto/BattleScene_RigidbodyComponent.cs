using UnityEngine;
namespace HotAssembly
{
    public partial class BattleScene_RigidbodyComponent
    {
        public GameObject obj;
        public GameObject rigidbodyObj = null;
        public UnityEngine.Transform startTransform = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            rigidbodyObj = allData[0].gameObject;
            startTransform = allData[1].exportComponent[0] as UnityEngine.Transform;
        }
    }
}
