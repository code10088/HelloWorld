using System;
using System.Collections.Generic;

public class ProcessControl<T> where T : ProcessItem, new()
{
    private List<T> list = new List<T>();
    private int index = 0;
    public int CurId => index < list.Count ? list[index].Id : int.MaxValue;

    public void Add(int id, Action<int> action = null, bool single = true)
    {
        if (single && list.Exists(a => a.Id == id)) return;
        var item = new T();
        item.Init(id, action);
        list.Add(item);
    }
    public void Remove(int id)
    {
        var index = list.FindIndex(a => a.Id == id);
        if (index >= 0) list.RemoveAt(index);
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
    public void Goto(int id)
    {
        var index = list.FindIndex(a => a.Id == id);
        if (index < 0) return;
        this.index = index;
        list[index].Excute();
    }
}
public class ProcessItem
{
    private int id;
    private Action<int> action;
    public int Id => id;

    public void Init(int id, Action<int> action)
    {
        this.id = id;
        this.action = action;
    }
    public virtual void Excute()
    {
        action?.Invoke(id);
    }
}