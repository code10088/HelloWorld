using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HotAssembly
{
    public class UIHotUpdateRes : UIBase
    {
        public UIHotUpdateResComponent component = new UIHotUpdateResComponent();
        private int loadId = -1;

        public override void InitUI(GameObject UIObj, UIType type, UIType from, Data_UIConfig config, params object[] param)
        {
            base.InitUI(UIObj, type, from, config, param);
            component.Init(UIObj);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            AssetManager.Instance.Unload(loadId);
        }
        public void SetBg(string name)
        {
            AssetManager.Instance.Unload(loadId);
            loadId = AssetManager.Instance.Load<Texture>(name, LoadBg);
        }
        private void LoadBg(int id, Object asset)
        {
            component.bgRawImage.texture = asset as Texture;
        }
        public void SetText(string str)
        {
            component.tipsTextMeshProUGUI.text = str;
        }
        public void SetSlider(float progress)
        {
            progress = float.IsNaN(progress) ? 0 : progress;
            component.sliderSlider.value = progress;
        }
    }
}