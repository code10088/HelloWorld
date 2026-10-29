public class UILogin : UIBase
{
    private UILoginComponent comp;
    private bool login = false;

    protected override void Init()
    {
        base.Init();
        comp = component as UILoginComponent;
        comp.qQLoginButton.onClick.AddListener(OnClickQQ);

    }

    private void OnClickQQ()
    {
        if (login)
        {
            UICommonTips.ShowTips(LanguageManager.Instance.Get(10009));
            return;
        }
        else
        {
            login = true;
            OnQQAuthCode("1");
        }
    }
    private void OnQQAuthCode(string authCode)
    {
        AccountInfo.Instance.LoginWithCode(LoginPlatform.QQ, authCode);
    }
}
