using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameObjectPool
{
    private Dictionary<string, GameObjectPool<GameObjectPoolItem>> pool = new();
    public void Enqueue(string path, int itemId)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (pool.TryGetValue(path, out var temp)) temp.Enqueue(itemId);
    }
    public GameObjectPoolItem Dequeue(string path, Transform parent, Action<int, GameObject, object[]> action = null, params object[] param)
    {
        if (string.IsNullOrEmpty(path)) return null;
        GameObjectPool<GameObjectPoolItem> temp = null;
        if (!pool.TryGetValue(path, out temp))
        {
            temp = new GameObjectPool<GameObjectPoolItem>();
            temp.Init(path);
            pool.Add(path, temp);
        }
        return temp.Dequeue(parent, action, param);
    }
    public void Release()
    {
        foreach (var item in pool) item.Value.Release();
        pool.Clear();
    }
}
public class GameObjectPool<T> : LoadAssetItem where T : GameObjectPoolItem, new()
{
    private List<T> use = new();
    private List<T> wait = new();
    private List<T> cache = new();
    private int cacheCount = 10;

    public List<T> Use => use;
    public int CacheCount => cacheCount;

    public void Enqueue(int itemId)
    {
        T temp = use.Find(a => a.ItemID == itemId);
        if (temp == null) return;
        use.Remove(temp);
        wait.Remove(temp);
        if (cache.Count >= cacheCount) temp.Release();
        else { temp.Delay(); cache.Add(temp); }
    }
    public T Dequeue(Transform parent, Action<int, GameObject, object[]> action = null, params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(parent, Delete, action, param);
        use.Add(temp);
        wait.Add(temp);
        Load<GameObject>();
        return temp;
    }
    public override void Release()
    {
        for (int i = use.Count - 1; i >= 0; i--) use[i].Release();
        for (int i = cache.Count - 1; i >= 0; i--) cache[i].Release();
        use.Clear();
        wait.Clear();
        cache.Clear();
        base.Release();
    }
    private void Delete(int itemId)
    {
        int index = wait.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            wait.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) Delay();
            return;
        }
        index = cache.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            cache.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) Delay();
            return;
        }
    }
    protected override void Finish(Object asset)
    {
        for (int i = 0; i < wait.Count; i++) wait[i].InstantiateAsync(asset);
        wait.Clear();
    }
}
public class GameObjectPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    private Transform parent;
    protected Object asset;
    private AsyncInstantiateOperation<Object> aio;
    protected GameObject obj;
    private LoadState state = LoadState.None;
    private int timerId = -1;

    private Action<int> release;
    private Action<int, GameObject, object[]> action;
    protected object[] param;

    public int ItemID => itemId;

    [Obsolete("请使用GameObjectPool")]
    public void Init(Transform parent, Action<int> release, Action<int, GameObject, object[]> action, params object[] param)
    {
        itemId = ++uniqueId;
        this.parent = parent;
        this.release = release;
        this.action = action;
        this.param = param;
    }
    [Obsolete("请使用GameObjectPool")]
    public void InstantiateAsync(Object _asset)
    {
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else if (state == LoadState.None)
        {
            asset = _asset;
            state = LoadState.Instantiating;
            aio = Object.InstantiateAsync(_asset);
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
            Release();
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
        if (obj != null)
        {
            obj.SetActive(true);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }
        if (action != null)
        {
            action?.Invoke(itemId, obj, param);
        }
    }
    public virtual void Delay()
    {
        obj?.SetActive(false);
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(GameSetting.recycleTimeS, finish: Release);
        state |= LoadState.Release;
        action = null;
        param = null;
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Release()
    {
        itemId = -1;
        parent = null;
        asset = null;
        aio = null;
        if (obj != null) GameObject.Destroy(obj);
        obj = null;
        state = LoadState.None;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
        release?.Invoke(itemId);
        release = null;
    }
    private void Recycle()
    {
        state &= LoadState.InstantiateFinish | LoadState.Instantiating;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
}


public class AssetPool
{
    private Dictionary<string, AssetPool<AssetPoolItem>> pool = new();
    public void Enqueue(string path, int itemId)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (pool.TryGetValue(path, out var temp)) temp.Enqueue(itemId);
    }
    public AssetPoolItem Dequeue<T>(string path, Action<int, Object, object[]> action = null, params object[] param) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;
        AssetPool<AssetPoolItem> temp = null;
        if (!pool.TryGetValue(path, out temp))
        {
            temp = new AssetPool<AssetPoolItem>();
            temp.Init(path);
            pool.Add(path, temp);
        }
        return temp.Dequeue<T>(action, param);
    }
    public void Release()
    {
        foreach (var item in pool) item.Value.Release();
        pool.Clear();
    }
}
public class AssetPool<T> : LoadAssetItem where T : AssetPoolItem, new()
{
    private List<T> use = new();
    private List<T> wait = new();
    private List<T> cache = new();
    private int cacheCount = 10;

    public List<T> Use => use;
    public int CacheCount => cacheCount;

    public void Enqueue(int itemId)
    {
        T temp = use.Find(a => a.ItemID == itemId);
        if (temp == null) return;
        use.Remove(temp);
        wait.Remove(temp);
        if (cache.Count >= cacheCount) temp.Release();
        else { temp.Delay(); cache.Add(temp); }
    }
    public T Dequeue<K>(Action<int, Object, object[]> action = null, params object[] param) where K : Object
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(Delete, action, param);
        use.Add(temp);
        wait.Add(temp);
        Load<K>();
        return temp;
    }
    public override void Release()
    {
        for (int i = use.Count - 1; i >= 0; i--) use[i].Release();
        for (int i = cache.Count - 1; i >= 0; i--) cache[i].Release();
        use.Clear();
        wait.Clear();
        cache.Clear();
        base.Release();
    }
    private void Delete(int itemId)
    {
        int index = wait.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            wait.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) Delay();
            return;
        }
        index = cache.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            cache.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) Delay();
            return;
        }
    }
    protected override void Finish(Object asset)
    {
        for (int i = 0; i < wait.Count; i++) wait[i].InstantiateAsync(asset);
        wait.Clear();
    }
}
public class AssetPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    protected Object asset;
    private LoadState state = LoadState.None;
    private int timerId = -1;

    private Action<int> release;
    private Action<int, Object, object[]> action;
    protected object[] param;

    public int ItemID => itemId;

    [Obsolete("请使用AssetPool")]
    public void Init(Action<int> release, Action<int, Object, object[]> action, params object[] param)
    {
        itemId = ++uniqueId;
        this.release = release;
        this.action = action;
        this.param = param;
    }
    [Obsolete("请使用AssetPool")]
    public void InstantiateAsync(Object _asset)
    {
        if (state == LoadState.Release)
        {
            Recycle();
        }
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else
        {
            asset = _asset;
            Finish(asset);
        }
    }
    public virtual void Finish(Object asset)
    {
        action?.Invoke(itemId, asset, param);
    }
    public virtual void Delay()
    {
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(GameSetting.recycleTimeS, finish: Release);
        state = LoadState.Release;
        action = null;
        param = null;
    }
    [Obsolete("请使用AssetPool")]
    public virtual void Release()
    {
        itemId = -1;
        asset = null;
        state = LoadState.None;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
        release?.Invoke(itemId);
        release = null;
    }
    private void Recycle()
    {
        state = LoadState.None;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
}