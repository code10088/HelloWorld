public class UIMain : UIBase
{
    private UIMainComponent component = new UIMainComponent();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
    }
    protected override void PlayEnableAni()
    {
        base.PlayEnableAni();
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
}