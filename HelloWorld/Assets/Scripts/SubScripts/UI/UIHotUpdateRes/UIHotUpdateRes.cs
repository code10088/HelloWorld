public class UIHotUpdateRes : UIBase
{
    public UIHotUpdateResComponent comp;

    protected override void Init()
    {
        base.Init();
        comp = component as UIHotUpdateResComponent;
        comp.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    public void SetBg(string name)
    {
        SetSprite(comp.bgUIRawImage, name);
    }
    public void SetText(string str)
    {
        comp.tipsTextMeshProUGUI.text = str;
    }
    public void SetSlider(float progress)
    {
        progress = float.IsNaN(progress) ? 0 : progress;
        comp.sliderSlider.value = progress;
    }
}