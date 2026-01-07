using UnityEngine;
public partial class CommonItem_NormalComponent
{
    public GameObject obj;
    public GameObject qualityObj = null;
    public UnityEngine.UI.Image qualityImage = null;
    public UnityEngine.UI.Image iconImage = null;
    public GameObject numBgObj = null;
    public TMPro.TextMeshProUGUI numTextMeshProUGUI = null;
    public TMPro.TextMeshProUGUI nameTextMeshProUGUI = null;
    public GameObject lockObj = null;
    public TMPro.TextMeshProUGUI lockLvTextMeshProUGUI = null;
    public GameObject receivedObj = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        qualityObj = allData[0].gameObject;
        qualityImage = allData[0].exportComponent[1] as UnityEngine.UI.Image;
        iconImage = allData[1].exportComponent[0] as UnityEngine.UI.Image;
        numBgObj = allData[2].gameObject;
        numTextMeshProUGUI = allData[3].exportComponent[0] as TMPro.TextMeshProUGUI;
        nameTextMeshProUGUI = allData[4].exportComponent[0] as TMPro.TextMeshProUGUI;
        lockObj = allData[5].gameObject;
        lockLvTextMeshProUGUI = allData[6].exportComponent[0] as TMPro.TextMeshProUGUI;
        receivedObj = allData[7].gameObject;
    }
}
