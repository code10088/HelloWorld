using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoSingleton<EffectManager>, SingletonInterface
{
    private static Dictionary<string, ObjectPool<EffectItem>> pool = new();

    public void Init()
    {
        gameObject.SetActive(false);
    }

    public EffectItem Get(string path, Transform parent, Action<GameObject, object[]> action = null, float time = 0, params object[] param)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!pool.TryGetValue(path, out var temp))
        {
            temp = new();
            temp.Init(path);
            pool.Add(path, temp);
        }
        var item = temp.Get(action, param);
        item.Init(path, parent, time);
        return item;
    }
    public void Return(EffectItem item)
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
public class EffectItem : ObjectPoolItem
{
    private string path;
    private Transform parent;
    private int timerId = -1;

    public string Path => path;

    public void Init(string path, Transform parent, float time = 0)
    {
        this.path = path;
        this.parent = parent;
        if (time > 0) timerId = Driver.Instance.StartTimer(time, finish: Recycle);
    }
    protected override void Finish(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.parent = parent;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        base.Finish(obj);
    }
    public override void Disable()
    {
        base.Disable();
        if (obj) obj.transform.parent = EffectManager.Instance.transform;
        Driver.Instance.Remove(timerId);
        timerId = -1;
    }
    private void Recycle()
    {
        EffectManager.Instance.Return(this);
    }
}