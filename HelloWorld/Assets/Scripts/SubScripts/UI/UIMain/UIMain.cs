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
    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        EventManager.Instance.Register<int>(EventType.RefreshDelay, RefreshDelay);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        EventManager.Instance.Unregister<int>(EventType.RefreshDelay, RefreshDelay);
    }

    private void RefreshDelay(int delay)
    {
        comp.delayTextMeshProUGUI.text = delay + "ms";
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