using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool<T> where T : ObjectPoolItem, new()
{
    private int loadId = -1;
    private Object asset;
    private List<T> wait = new();
    private Queue<T> cache = new();
    private int cacheCount = 10;

    public void Init(GameObject obj, int cacheCount = 10)
    {
        asset = obj;
        this.cacheCount = cacheCount;
    }
    public void Init(string path, int cacheCount = 10)
    {
        this.cacheCount = cacheCount;
        AssetManager.Instance.Load<GameObject>(ref loadId, path, LoadFinish);
    }
    private void LoadFinish(int loadId, Object asset)
    {
        this.asset = asset;
        for (int i = 0; i < wait.Count; i++) wait[i].Enable(asset);
        wait.Clear();
    }

    public T Get(Action<GameObject, object[]> action = null, params object[] param)
    {
        T temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init(action, param);
        if (asset == null) wait.Add(temp);
        else temp.Enable(asset);
        return temp;
    }
    public void Return(T item)
    {
        if (item == null) return;
        wait.Remove(item);
        if (cache.Count >= cacheCount) item.Destroy();
        else { item.Disable(); cache.Enqueue(item); }
    }
    public void Clear()
    {
        AssetManager.Instance.Unload(ref loadId);
        asset = null;
        wait.Clear();
        while (cache.Count > 0) cache.Dequeue().Destroy();
    }
}
public class ObjectPoolItem
{
    private AsyncInstantiateOperation<Object> aio;
    private LoadState state = LoadState.None;
    protected GameObject obj;

    private Action<GameObject, object[]> action;
    protected object[] param;

    [Obsolete("请使用GameObjectPool")]
    public void Init(Action<GameObject, object[]> action, params object[] param)
    {
        this.action = action;
        this.param = param;
    }
    [Obsolete("请使用GameObjectPool")]
    public void Enable(Object asset)
    {
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
        if (asset == null)
        {
            Destroy();
            Finish(null);
        }
        else if (state == LoadState.None)
        {
            state = LoadState.Instantiating;
            aio = Object.InstantiateAsync(asset);
            aio.completed += InstantiateFinish;
        }
        else if (state == LoadState.InstantiateFinish)
        {
            Finish(obj);
        }
    }
    private void InstantiateFinish(AsyncOperation operation)
    {
        if (aio.Result.Length == 0)
        {
            Destroy();
            Finish(null);
        }
        else if (state.HasFlag(LoadState.Release))
        {
            obj = aio.Result[0] as GameObject;
            state = LoadState.InstantiateFinish | LoadState.Release;
            obj.SetActive(false);
            Finish(null);
        }
        else
        {
            obj = aio.Result[0] as GameObject;
            state = LoadState.InstantiateFinish;
            Finish(obj);
        }
    }
    protected virtual void Finish(GameObject obj)
    {
        obj?.SetActive(true);
        action?.Invoke(obj, param);
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Disable()
    {
        obj?.SetActive(false);
        state |= LoadState.Release;
        action = null;
        param = null;
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Destroy()
    {
        aio = null;
        state = LoadState.None;
        if (obj) GameObject.Destroy(obj);
        obj = null;
        action = null;
        param = null;
    }
    private void Recycle()
    {
        state &= LoadState.InstantiateFinish | LoadState.Instantiating;
    }
}