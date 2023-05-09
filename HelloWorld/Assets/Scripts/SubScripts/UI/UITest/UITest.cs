using UnityEngine;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        public override void InitUI(GameObject UIObj, UIType type, UIType from, Data_UIConfig config, params object[] param)
        {
            GameDebug.Log("UITest InitUI");
            base.InitUI(UIObj, type, from, config, param);
            component.Init(UIObj);

            component.buttonButton.onClick.AddListener(OnClickClose);
        }
        public override void Refresh(params object[] param)
        {
            GameDebug.Log("UITest Refresh");
            base.Refresh(param);
        }
        public override void PlayInitAni()
        {
            GameDebug.Log("UITest OnPlayUIAnimation");
            base.PlayInitAni();
        }
        public override void OnDestroy()
        {
            GameDebug.Log("UITest OnDestroy");
            base.OnDestroy();
        }

        private void OnClickClose()
        {
            GameDebug.Log("UITest Click");
            UIMessageBoxParam param = new UIMessageBoxParam();
            param.type = UIMessageBoxType.SureAndCancel;
            param.title = "Tips";
            param.content = "Content";
            param.sure = a => OnClickClose();
            UIManager.Instance.OpenMessageBox(param);
        }
    }
}