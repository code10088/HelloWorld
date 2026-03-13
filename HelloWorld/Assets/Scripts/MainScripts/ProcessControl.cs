using System.Collections.Generic;

public class ProcessControl
{
    private List<ProcessItem> list = new List<ProcessItem>();
    private int index = 0;

    public ProcessItem Cur => index < list.Count ? list[index] : null;

    public void Add(ProcessItem item)
    {
        list.Add(item);
    }
    public void Remove(ProcessItem item)
    {
        list.Remove(item);
    }
    public void Start()
    {
        index = -1;
        Next();
    }
    public void Next()
    {
        index++;
        if (index < list.Count) list[index].Excute();
        else list.Clear();
    }
    public void Goto(ProcessItem item)
    {
        var index = list.IndexOf(item);
        if (index < 0) return;
        this.index = index;
        list[index].Excute();
    }
}