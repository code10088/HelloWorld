public class UISceneLoading : UIBase
{
    public UISceneLoadingComponent comp;

    protected override void Init()
    {
        base.Init();
        comp = component as UISceneLoadingComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
    }
    public override void OnEnable(params object[] param)
    {
        base.OnEnable();
        EventManager.Instance.Register<string>(EventType.SetSceneLoadingBg, SetBg);
        EventManager.Instance.Register<string, float>(EventType.SetSceneLoadingProgress, Refresh);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.Unregister<string>(EventType.SetSceneLoadingBg, SetBg);
        EventManager.Instance.Unregister<string, float>(EventType.SetSceneLoadingProgress, Refresh);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    private void SetBg(string name)
    {
        SetSprite(comp.bgUIRawImage, name);
    }
    private void Refresh(string str, float progress)
    {
        comp.tipsTextMeshProUGUI.text = str;
        progress = float.IsNaN(progress) ? 0 : progress;
        comp.sliderSlider.value = progress;
    }
}