using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolList
{
    private Dictionary<string, ObjectPool<ObjectPoolListItem>> pool = new();
    public ObjectPoolListItem Get(string path, Action<GameObject, object[]> action = null, int cacheCount = 10, params object[] param)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!pool.TryGetValue(path, out var temp))
        {
            temp = new();
            temp.Init(path, cacheCount);
            pool.Add(path, temp);
        }
        var item = temp.Get(action, param);
        item.Init(path);
        return item;
    }
    public void Return(ObjectPoolListItem item)
    {
        if (item == null) return;
        if (pool.TryGetValue(item.Path, out var temp)) temp.Return(item);
    }
    public void Clear()
    {
        foreach (var item in pool) item.Value.Clear();
        pool.Clear();
    }
}
public class ObjectPoolListItem : ObjectPoolItem
{
    private string path;
    public string Path => path;
    public void Init(string path)
    {
        this.path = path;
    }
}