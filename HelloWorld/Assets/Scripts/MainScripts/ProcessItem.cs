using System;

public interface ProcessItem
{
    public void Excute();
}
public class ActionProcessItem : ProcessItem
{
    private Action action;

    public ActionProcessItem(Action action)
    {
        this.action = action;
    }
    public void Excute()
    {
        action?.Invoke();
    }
}