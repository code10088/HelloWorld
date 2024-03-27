using UnityEngine;
namespace HotAssembly
{
    public partial class UIGuideComponent
    {
        public GameObject obj;
        public UnityEngine.RectTransform maskRectTransform = null;
        public UnityEngine.MeshFilter maskMeshFilter = null;
        public UnityEngine.MeshRenderer maskMeshRenderer = null;
        public UIButton maskUIButton = null;
        public GameObject skipBtnObj = null;
        public UIButton skipBtnUIButton = null;
        public void Init(GameObject obj)
        {
            this.obj = obj;
            ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
            maskRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
            maskMeshFilter = allData[0].exportComponent[1] as UnityEngine.MeshFilter;
            maskMeshRenderer = allData[0].exportComponent[2] as UnityEngine.MeshRenderer;
            maskUIButton = allData[0].exportComponent[3] as UIButton;
            skipBtnObj = allData[1].gameObject;
            skipBtnUIButton = allData[1].exportComponent[1] as UIButton;
        }
    }
}
