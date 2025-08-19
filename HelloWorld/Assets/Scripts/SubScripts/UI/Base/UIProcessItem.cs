using cfg;

public class UIProcessItem : ProcessItem
{
    public override void Excute()
    {
        UIManager.Instance.OpenUI((UIType)Id);
    }
}
