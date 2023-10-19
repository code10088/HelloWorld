using cfg;
using Cysharp.Threading.Tasks;

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
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
            await UniTask.Delay(1000);
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UITest OnPlayUIAnimation");
        }
        protected override void RefreshUILayer()
        {
            base.RefreshUILayer();
            GameDebug.Log("UITest RefreshUILayer");
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