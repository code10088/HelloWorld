using cfg;

namespace HotAssembly
{
    public class UIMain : UIBase
    {
        private UIMainComponent component = new UIMainComponent();

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.openUITestBtnUIButton.onClick.AddListener(OnOpenUITest);
            component.openUISettingUIButton.onClick.AddListener(OnOpenUISetting);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
        }
        protected override void RefreshUILayer()
        {
            base.RefreshUILayer();
        }
        public override void OnDisable()
        {
            base.OnDisable();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }
        private void OnOpenUITest()
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
        private void OnOpenUISetting()
        {
            UIManager.Instance.OpenUI(UIType.UISetting);
        }
    }
}