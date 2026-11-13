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
#if UNITY_EDITOR
            OnQQLogin("0|0|0|0");
#else
            SDK.Instance.QQLogin(OnQQLogin);
#endif
        }
    }
    private void OnQQLogin(string content)
    {
        login = false;
        //成功 "0|openid|access_token|expires_in"，取消 "-3"，失败 "-4|code|msg"
        if (string.IsNullOrEmpty(content) || content[0] != '0')
        {
            UICommonTips.ShowTips(LanguageManager.Instance.Get(10008));
        }
        else
        {
            var args = content.Split('|');
            AccountInfo.Instance.LoginWithCode(LoginPlatform.QQ, args[2]);
            OnClose();
        }
    }
}
