using UnityEngine;
public partial class UIMessageBoxComponent
{
    public GameObject obj;
    public TMPro.TextMeshProUGUI titleTextMeshProUGUI = null;
    public TMPro.TextMeshProUGUI contentTextMeshProUGUI = null;
    public UnityEngine.UI.Button sure1Button = null;
    public UnityEngine.UI.Button sure2Button = null;
    public UnityEngine.UI.Button cancelButton = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        titleTextMeshProUGUI = allData[0].exportComponent[0] as TMPro.TextMeshProUGUI;
        contentTextMeshProUGUI = allData[1].exportComponent[0] as TMPro.TextMeshProUGUI;
        sure1Button = allData[2].exportComponent[0] as UnityEngine.UI.Button;
        sure2Button = allData[3].exportComponent[0] as UnityEngine.UI.Button;
        cancelButton = allData[4].exportComponent[0] as UnityEngine.UI.Button;
    }
}
