using UnityEngine;
public partial class UISettingComponent
{
    public GameObject obj;
    public UnityEngine.RectTransform bgRectTransform = null;
    public UIButton closeBtnUIButton = null;
    public UnityEngine.UI.Toggle qualitySettings0Toggle = null;
    public UnityEngine.UI.Toggle qualitySettings1Toggle = null;
    public UnityEngine.UI.Toggle qualitySettings2Toggle = null;
    public UnityEngine.UI.Toggle frameRate0Toggle = null;
    public UnityEngine.UI.Toggle frameRate1Toggle = null;
    public UnityEngine.UI.Toggle frameRate2Toggle = null;
    public UnityEngine.UI.Toggle screenResolution0Toggle = null;
    public UnityEngine.UI.Toggle screenResolution1Toggle = null;
    public UnityEngine.UI.Toggle screenResolution2Toggle = null;
    public UnityEngine.UI.Toggle masterTextureLimit0Toggle = null;
    public UnityEngine.UI.Toggle antiLv0Toggle = null;
    public UnityEngine.UI.Toggle shadow0Toggle = null;
    public UnityEngine.UI.Toggle softShadow0Toggle = null;
    public UnityEngine.UI.Toggle shadowLv0Toggle = null;
    public UnityEngine.UI.Toggle shadowLv1Toggle = null;
    public UnityEngine.UI.Toggle shadowLv2Toggle = null;
    public UnityEngine.UI.Toggle hDR0Toggle = null;
    public UnityEngine.UI.Toggle postProcess0Toggle = null;
    public UnityEngine.UI.Toggle graphicsQualityLv0Toggle = null;
    public UnityEngine.UI.Toggle graphicsQualityLv1Toggle = null;
    public UnityEngine.UI.Toggle graphicsQualityLv2Toggle = null;
    public void Init(GameObject obj)
    {
        this.obj = obj;
        ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);
        bgRectTransform = allData[0].exportComponent[0] as UnityEngine.RectTransform;
        closeBtnUIButton = allData[1].exportComponent[0] as UIButton;
        qualitySettings0Toggle = allData[2].exportComponent[0] as UnityEngine.UI.Toggle;
        qualitySettings1Toggle = allData[3].exportComponent[0] as UnityEngine.UI.Toggle;
        qualitySettings2Toggle = allData[4].exportComponent[0] as UnityEngine.UI.Toggle;
        frameRate0Toggle = allData[5].exportComponent[0] as UnityEngine.UI.Toggle;
        frameRate1Toggle = allData[6].exportComponent[0] as UnityEngine.UI.Toggle;
        frameRate2Toggle = allData[7].exportComponent[0] as UnityEngine.UI.Toggle;
        screenResolution0Toggle = allData[8].exportComponent[0] as UnityEngine.UI.Toggle;
        screenResolution1Toggle = allData[9].exportComponent[0] as UnityEngine.UI.Toggle;
        screenResolution2Toggle = allData[10].exportComponent[0] as UnityEngine.UI.Toggle;
        masterTextureLimit0Toggle = allData[11].exportComponent[0] as UnityEngine.UI.Toggle;
        antiLv0Toggle = allData[12].exportComponent[0] as UnityEngine.UI.Toggle;
        shadow0Toggle = allData[13].exportComponent[0] as UnityEngine.UI.Toggle;
        softShadow0Toggle = allData[14].exportComponent[0] as UnityEngine.UI.Toggle;
        shadowLv0Toggle = allData[15].exportComponent[0] as UnityEngine.UI.Toggle;
        shadowLv1Toggle = allData[16].exportComponent[0] as UnityEngine.UI.Toggle;
        shadowLv2Toggle = allData[17].exportComponent[0] as UnityEngine.UI.Toggle;
        hDR0Toggle = allData[18].exportComponent[0] as UnityEngine.UI.Toggle;
        postProcess0Toggle = allData[19].exportComponent[0] as UnityEngine.UI.Toggle;
        graphicsQualityLv0Toggle = allData[20].exportComponent[0] as UnityEngine.UI.Toggle;
        graphicsQualityLv1Toggle = allData[21].exportComponent[0] as UnityEngine.UI.Toggle;
        graphicsQualityLv2Toggle = allData[22].exportComponent[0] as UnityEngine.UI.Toggle;
    }
}
