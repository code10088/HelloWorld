using UnityEngine;
namespace HotAssembly
{
    public partial class UITestComponent
    {
        public GameObject obj;
        public UnityEngine.UI.Button buttonButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            buttonButton = allData[0].exportComponent[0] as UnityEngine.UI.Button;
        }
    }
}
