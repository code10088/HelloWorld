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
            component.buttonUIButton.onClick.AddListener(OnClickClose);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            GameDebug.Log("UISubTest OnEnable");
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UISubTest OnPlayUIAnimation");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("UISubTest OnDisable");
        }
        public override void OnDestroy()
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