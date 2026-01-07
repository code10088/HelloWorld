using cfg;

public class UIMain : UIBase
{
    private UIMainComponent component = new UIMainComponent();

    protected override void Init()
    {
        base.Init();
        component.Init(UIObj);
        component.uIMailBtnUIButton.onClick.AddListener(OnClickMail);
        component.uITestBtnUIButton.onClick.AddListener(OnClickTest);

    }

    private void OnClickMail()
    {
        DataManager.Instance.MailData.CSMail();
        UIManager.Instance.OpenUI(UIType.UIMail);
    }
    private void OnClickTest()
    {
        UIManager.Instance.OpenUI(UIType.UITest);
    }
}