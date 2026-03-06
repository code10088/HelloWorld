public class UISetting : UIBase
{
    private UISettingComponent comp;

    protected override void Init()
    {
        base.Init();
        comp = component as UISettingComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        comp.closeBtnUIButton.onClick.AddListener(OnClose);
        comp.qualitySettings0Toggle.onValueChanged.AddListener(OnClickQualitySettings0);
        comp.qualitySettings1Toggle.onValueChanged.AddListener(OnClickQualitySettings1);
        comp.qualitySettings2Toggle.onValueChanged.AddListener(OnClickQualitySettings2);
        comp.frameRate0Toggle.onValueChanged.AddListener(OnClickFrameRate0);
        comp.frameRate1Toggle.onValueChanged.AddListener(OnClickFrameRate1);
        comp.frameRate2Toggle.onValueChanged.AddListener(OnClickFrameRate2);
        comp.screenResolution0Toggle.onValueChanged.AddListener(OnClickScreenResolution0);
        comp.screenResolution1Toggle.onValueChanged.AddListener(OnClickScreenResolution1);
        comp.screenResolution2Toggle.onValueChanged.AddListener(OnClickScreenResolution2);
        comp.masterTextureLimit0Toggle.onValueChanged.AddListener(OnClickMasterTextureLimit0);
        comp.antiLv0Toggle.onValueChanged.AddListener(OnClickAntiLv0);
        comp.shadow0Toggle.onValueChanged.AddListener(OnClickShadow0);
        comp.softShadow0Toggle.onValueChanged.AddListener(OnClickSoftShadow0);
        comp.shadowLv0Toggle.onValueChanged.AddListener(OnClickShadowLv0);
        comp.shadowLv1Toggle.onValueChanged.AddListener(OnClickShadowLv1);
        comp.shadowLv2Toggle.onValueChanged.AddListener(OnClickShadowLv2);
        comp.hDR0Toggle.onValueChanged.AddListener(OnClickHDR0);
        comp.postProcess0Toggle.onValueChanged.AddListener(OnClickPostProcess0);
        comp.graphicsQualityLv0Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv0);
        comp.graphicsQualityLv1Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv1);
        comp.graphicsQualityLv2Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv2);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        int lv = DPUtil.QualityLv;
        if (lv == 0) comp.qualitySettings0Toggle.isOn = true;
        else if (lv == 1) comp.qualitySettings1Toggle.isOn = true;
        else if (lv == 2) comp.qualitySettings2Toggle.isOn = true;
        lv = DPUtil.FrameRate;
        if (lv == 0) comp.frameRate0Toggle.isOn = true;
        else if (lv == 1) comp.frameRate1Toggle.isOn = true;
        else if (lv == 2) comp.frameRate2Toggle.isOn = true;
        lv = DPUtil.ScreenResolution;
        if (lv == 0) comp.screenResolution0Toggle.isOn = true;
        else if (lv == 1) comp.screenResolution1Toggle.isOn = true;
        else if (lv == 2) comp.screenResolution2Toggle.isOn = true;
        lv = DPUtil.MasterTextureLimit;
        comp.masterTextureLimit0Toggle.isOn = lv == 1;
        lv = DPUtil.AntiLv;
        comp.antiLv0Toggle.isOn = lv == 1;
        lv = DPUtil.Shadow;
        comp.shadow0Toggle.isOn = lv == 1;
        lv = DPUtil.SoftShadow;
        comp.softShadow0Toggle.isOn = lv == 1;
        lv = DPUtil.ShadowLv;
        if (lv == 0) comp.shadowLv0Toggle.isOn = true;
        else if (lv == 1) comp.shadowLv1Toggle.isOn = true;
        else if (lv == 2) comp.shadowLv2Toggle.isOn = true;
        lv = DPUtil.HDR;
        comp.hDR0Toggle.isOn = lv == 1;
        lv = DPUtil.PostProcess;
        comp.postProcess0Toggle.isOn = lv == 1;
        lv = DPUtil.GraphicsQualityLv;
        if (lv == 0) comp.graphicsQualityLv0Toggle.isOn = true;
        else if (lv == 1) comp.graphicsQualityLv1Toggle.isOn = true;
        else if (lv == 2) comp.graphicsQualityLv2Toggle.isOn = true;
    }
    private void OnClickQualitySettings0(bool b)
    {
        if (b) DPUtil.SetQualitySettings(0);
    }
    private void OnClickQualitySettings1(bool b)
    {
        if (b) DPUtil.SetQualitySettings(1);
    }
    private void OnClickQualitySettings2(bool b)
    {
        if (b) DPUtil.SetQualitySettings(2);
    }
    private void OnClickFrameRate0(bool b)
    {
        if (b) DPUtil.SetFrameRate(0);
    }
    private void OnClickFrameRate1(bool b)
    {
        if (b) DPUtil.SetFrameRate(1);
    }
    private void OnClickFrameRate2(bool b)
    {
        if (b) DPUtil.SetFrameRate(2);
    }
    private void OnClickScreenResolution0(bool b)
    {
        if (b) DPUtil.SetScreenResolution(0);
    }
    private void OnClickScreenResolution1(bool b)
    {
        if (b) DPUtil.SetScreenResolution(1);
    }
    private void OnClickScreenResolution2(bool b)
    {
        if (b) DPUtil.SetScreenResolution(2);
    }
    private void OnClickMasterTextureLimit0(bool b)
    {
        DPUtil.SetMasterTextureLimit(b ? 1 : 0);
    }
    private void OnClickAntiLv0(bool b)
    {
        DPUtil.SetAntiLv(b ? 1 : 0);
    }
    private void OnClickShadow0(bool b)
    {
        DPUtil.SetShadow(b ? 1 : 0);
    }
    private void OnClickSoftShadow0(bool b)
    {
        DPUtil.SetSoftShadow(b ? 1 : 0);
    }
    private void OnClickShadowLv0(bool b)
    {
        if (b) DPUtil.SetShadowLv(0);
    }
    private void OnClickShadowLv1(bool b)
    {
        if (b) DPUtil.SetShadowLv(1);
    }
    private void OnClickShadowLv2(bool b)
    {
        if (b) DPUtil.SetShadowLv(2);
    }
    private void OnClickHDR0(bool b)
    {
        DPUtil.SetHDR(b ? 1 : 0);
    }
    private void OnClickPostProcess0(bool b)
    {
        DPUtil.SetPostProcess(b ? 1 : 0);
    }
    private void OnClickGraphicsQualityLv0(bool b)
    {
        if (b) DPUtil.SetGraphicsQualityLv(0);
    }
    private void OnClickGraphicsQualityLv1(bool b)
    {
        if (b) DPUtil.SetGraphicsQualityLv(1);
    }
    private void OnClickGraphicsQualityLv2(bool b)
    {
        if (b) DPUtil.SetGraphicsQualityLv(2);
    }
}