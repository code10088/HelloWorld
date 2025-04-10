public class UIHotUpdateRes : UIBase
{
    public UIHotUpdateResComponent component = new UIHotUpdateResComponent();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    public void SetBg(string name)
    {
        SetSprite(component.bgUIRawImage, name);
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