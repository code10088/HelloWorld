using TMPro;

public class UIText : TextMeshProUGUI
{
    public int i18nKey;
    protected override void Awake()
    {
        base.Awake();
        if (i18nKey > 0) text = LanguageManager.Instance.Get(i18nKey);
    }
    public void SetText(int key, params object[] args)
    {
        text = LanguageManager.Instance.Get(key, args);
    }
}
