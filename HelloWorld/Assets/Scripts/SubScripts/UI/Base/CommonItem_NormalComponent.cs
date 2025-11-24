using UnityEngine;
public partial class CommonItem_NormalComponent
{
    public GameObject obj;
    public GameObject qualityObj = null;
    public UIImage qualityUIImage = null;
    public UIImage iconUIImage = null;
    public GameObject numBgObj = null;
    public TMPro.TextMeshProUGUI numTextMeshProUGUI = null;
    public TMPro.TextMeshProUGUI nameTextMeshProUGUI = null;
    public GameObject lockObj = null;
    public TMPro.TextMeshProUGUI lockLvTextMeshProUGUI = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        qualityObj = allData[0].gameObject;
        qualityUIImage = allData[0].exportComponent[1] as UIImage;
        iconUIImage = allData[1].exportComponent[0] as UIImage;
        numBgObj = allData[2].gameObject;
        numTextMeshProUGUI = allData[3].exportComponent[0] as TMPro.TextMeshProUGUI;
        nameTextMeshProUGUI = allData[4].exportComponent[0] as TMPro.TextMeshProUGUI;
        lockObj = allData[5].gameObject;
        lockLvTextMeshProUGUI = allData[6].exportComponent[0] as TMPro.TextMeshProUGUI;
    }
}
