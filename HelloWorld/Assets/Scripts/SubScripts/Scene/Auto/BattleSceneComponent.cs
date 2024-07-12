using UnityEngine;
namespace HotAssembly
{
    public partial class BattleSceneComponent
    {
        public GameObject obj;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        }
    }
}
