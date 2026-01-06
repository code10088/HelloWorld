using cfg;

public class UIMail : UIBase
{
    private UIMailComponent component = new UIMailComponent();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.bgRectTransform.anchorMin = UIManager.Instance.anchorMinFull;
        component.closeBtnUIButton.onClick.AddListener(OnClose);

    }
}