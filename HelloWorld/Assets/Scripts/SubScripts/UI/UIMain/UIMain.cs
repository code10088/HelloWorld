using cfg;

public class UIMain : UIBase
{
    private UIMainComponent comp;

    protected override void Init()
    {
        base.Init();
        comp = component as UIMainComponent;
        comp.uIMailBtnUIButton.onClick.AddListener(OnClickMail);
        comp.uITestBtnUIButton.onClick.AddListener(OnClickTest);

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