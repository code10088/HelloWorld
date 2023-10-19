using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HotAssembly
{
    public class UISetting : UIBase
    {
        private UISettingComponent component = new UISettingComponent();

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.bgRectTransform.anchorMin = UIManager.anchorMinFull;
            component.closeBtnUIButton.onClick.AddListener(OnClose);
            component.qualitySettings0Toggle.onValueChanged.AddListener(OnClickQualitySettings0);
            component.qualitySettings1Toggle.onValueChanged.AddListener(OnClickQualitySettings1);
            component.qualitySettings2Toggle.onValueChanged.AddListener(OnClickQualitySettings2);
            component.frameRate0Toggle.onValueChanged.AddListener(OnClickFrameRate0);
            component.frameRate1Toggle.onValueChanged.AddListener(OnClickFrameRate1);
            component.frameRate2Toggle.onValueChanged.AddListener(OnClickFrameRate2);
            component.screenResolution0Toggle.onValueChanged.AddListener(OnClickScreenResolution0);
            component.screenResolution1Toggle.onValueChanged.AddListener(OnClickScreenResolution1);
            component.screenResolution2Toggle.onValueChanged.AddListener(OnClickScreenResolution2);
            component.masterTextureLimit0Toggle.onValueChanged.AddListener(OnClickMasterTextureLimit0);
            component.antiLv0Toggle.onValueChanged.AddListener(OnClickAntiLv0);
            component.shadow0Toggle.onValueChanged.AddListener(OnClickShadow0);
            component.softShadow0Toggle.onValueChanged.AddListener(OnClickSoftShadow0);
            component.shadowLv0Toggle.onValueChanged.AddListener(OnClickShadowLv0);
            component.shadowLv1Toggle.onValueChanged.AddListener(OnClickShadowLv1);
            component.shadowLv2Toggle.onValueChanged.AddListener(OnClickShadowLv2);
            component.hDR0Toggle.onValueChanged.AddListener(OnClickHDR0);
            component.postProcess0Toggle.onValueChanged.AddListener(OnClickPostProcess0);
            component.graphicsQualityLv0Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv0);
            component.graphicsQualityLv1Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv1);
            component.graphicsQualityLv2Toggle.onValueChanged.AddListener(OnClickGraphicsQualityLv2);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            int lv = DPUtil.QualityLv;
            if (lv == 0) component.qualitySettings0Toggle.isOn = true;
            else if (lv == 1) component.qualitySettings1Toggle.isOn = true;
            else if (lv == 2) component.qualitySettings2Toggle.isOn = true;
            lv = DPUtil.FrameRate;
            if (lv == 0) component.frameRate0Toggle.isOn = true;
            else if (lv == 1) component.frameRate1Toggle.isOn = true;
            else if (lv == 2) component.frameRate2Toggle.isOn = true;
            lv = DPUtil.ScreenResolution;
            if (lv == 0) component.screenResolution0Toggle.isOn = true;
            else if (lv == 1) component.screenResolution1Toggle.isOn = true;
            else if (lv == 2) component.screenResolution2Toggle.isOn = true;
            lv = DPUtil.MasterTextureLimit;
            component.masterTextureLimit0Toggle.isOn = lv == 1;
            lv = DPUtil.AntiLv;
            component.antiLv0Toggle.isOn = lv == 1;
            lv = DPUtil.Shadow;
            component.shadow0Toggle.isOn = lv == 1;
            lv = DPUtil.SoftShadow;
            component.softShadow0Toggle.isOn = lv == 1;
            lv = DPUtil.ShadowLv;
            if (lv == 0) component.shadowLv0Toggle.isOn = true;
            else if (lv == 1) component.shadowLv1Toggle.isOn = true;
            else if (lv == 2) component.shadowLv2Toggle.isOn = true;
            lv = DPUtil.HDR;
            component.hDR0Toggle.isOn = lv == 1;
            lv = DPUtil.PostProcess;
            component.postProcess0Toggle.isOn = lv == 1;
            lv = DPUtil.GraphicsQualityLv;
            if (lv == 0) component.graphicsQualityLv0Toggle.isOn = true;
            else if (lv == 1) component.graphicsQualityLv1Toggle.isOn = true;
            else if (lv == 2) component.graphicsQualityLv2Toggle.isOn = true;
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
}