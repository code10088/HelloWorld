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
        EventManager.Instance.RegisterEvent(EventType.SetSceneLoadingBg, SetBg);
        EventManager.Instance.RegisterEvent(EventType.SetSceneLoadingProgress, Refresh);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.UnRegisterEvent(EventType.SetSceneLoadingBg, SetBg);
        EventManager.Instance.UnRegisterEvent(EventType.SetSceneLoadingProgress, Refresh);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    private void SetBg(object name)
    {
        SetSprite(comp.bgUIRawImage, (string)name);
    }
    private void Refresh(object info)
    {
        object[] temp = (object[])info;

        string str = (string)temp[0];
        comp.tipsTextMeshProUGUI.text = str;

        float progress = (float)temp[1];
        progress = float.IsNaN(progress) ? 0 : progress;
        comp.sliderSlider.value = progress;
    }
}