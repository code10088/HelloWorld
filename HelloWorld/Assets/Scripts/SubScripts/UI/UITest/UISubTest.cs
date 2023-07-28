using Cysharp.Threading.Tasks;

namespace HotAssembly
{
    public class UISubTest : UISubItem
    {
        private UISubTestComponent component = new UISubTestComponent();

        public UISubTest(string path) : base(path) { }
        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.buttonButton.onClick.AddListener(OnClickClose);
        }
        protected override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("UISubTest OnEnable");
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UISubTest OnPlayUIAnimation");
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("UISubTest OnDisable");
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameDebug.Log("UISubTest OnDestroy");
        }
        private void OnClickClose()
        {
            Close();
        }
    }
}