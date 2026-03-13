using cfg;

public class UIProcessItem : ProcessItem
{
    public UIType type;
    public UIProcessItem(UIType type)
    {
        this.type = type;
    }
    public void Excute()
    {
        UIManager.Instance.OpenUI(type);
    }
}
