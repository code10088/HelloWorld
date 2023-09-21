using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class AssetPool<T> : LoadAssetItem where T : AssetPoolItem, new()
{
    private List<T> use = new();
    private List<T> wait = new();

    public List<T> Use => use;

    public void Enqueue(T item)
    {
        int index = use.FindIndex(a => a.ItemID == item.ItemID);
        if (index >= 0) use.RemoveAt(index);
        index = wait.FindIndex(a => a.ItemID == item.ItemID);
        if (index >= 0) wait.RemoveAt(index);
        if (use.Count == 0) Delay();
    }
    public T Dequeue(Action<object[], Object> action = null, params object[] param)
    {
        T temp = new T();
        temp.Init(Delete, action, param);
        use.Add(temp);
        wait.Add(temp);
        Load<Object>();
        return temp;
    }
    public override void Release()
    {
        use.Clear();
        wait.Clear();
        base.Release();
    }
    private void Delete(int itemId)
    {
        int index = use.FindIndex(a => a.ItemID == itemId);
        if (index >= 0) use.RemoveAt(index);
        index = wait.FindIndex(a => a.ItemID == itemId);
        if (index >= 0) wait.RemoveAt(index);
        if (use.Count == 0) Delay();
    }
    protected override void Finish(Object asset)
    {
        if (asset == null) return;
        for (int i = 0; i < wait.Count; i++) wait[i].LoadFinish(asset);
        wait.Clear();
    }
}
public class AssetPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;

    private Action<int> release;
    private Action<object[], Object> action;
    protected object[] param;

    public int ItemID => itemId;

    public void Init(Action<int> release, Action<object[], Object> action, params object[] param)
    {
        itemId = ++uniqueId;
        this.release = release;
        this.action = action;
        this.param = param;
    }
    protected void Release()
    {
        release?.Invoke(itemId);
    }
    public virtual void LoadFinish(Object asset)
    {
        action?.Invoke(param, asset);
    }
}