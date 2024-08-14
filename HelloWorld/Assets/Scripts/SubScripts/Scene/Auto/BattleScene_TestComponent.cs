using UnityEngine;
namespace HotAssembly
{
    public partial class BattleScene_TestComponent
    {
        public GameObject obj;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        }
    }
}
