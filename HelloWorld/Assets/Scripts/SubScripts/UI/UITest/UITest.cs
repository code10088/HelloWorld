using UnityEngine;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();

        protected override void InitComponent()
        {
            component.Init(UIObj);
            component.buttonButton.onClick.AddListener(OnClickClose);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UITest OnPlayUIAnimation");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("UITest OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            GameDebug.Log("UITest OnDestroy");
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