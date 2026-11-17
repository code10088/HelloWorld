public class UIFriendRemark : UIBase
{
    private UIFriendRemarkComponent comp;
    private uint playerId;

    protected override void Init()
    {
        base.Init();
        comp = component as UIFriendRemarkComponent;
        comp.closeBtnUIButton.onClick.AddListener(OnClose);
        comp.cancelBtnUIButton.onClick.AddListener(OnClose);
        comp.sureBtnUIButton.onClick.AddListener(OnSure);
    }

    public override void OnEnable(params object[] param)
    {
        base.OnEnable(param);
        playerId = param != null && param.Length > 0 ? (uint)param[0] : 0;
        string remark = param != null && param.Length > 1 ? param[1] as string : string.Empty;
        comp.remarkInputField.characterLimit = 12;
        comp.remarkInputField.text = remark ?? string.Empty;
    }

    private void OnSure()
    {
        string text = comp.remarkInputField.text;
        if (text == null) text = string.Empty;
        text = text.Trim();
        if (text.Length > 12) text = text.Substring(0, 12);
        DataManager.Instance.FriendData.CSSetFriendRemark(playerId, text);
        OnClose();
    }
}