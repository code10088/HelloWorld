using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameObjectPool
{
    private Dictionary<int, GameObjectPool<ObjectPoolItem>> pool = new();
    public void Enqueue(GameObject obj, int itemId)
    {
        if (obj == null) return;
        var id = obj.GetInstanceID();
        if (pool.TryGetValue(id, out var temp)) temp.Enqueue(itemId);
    }
    public int Dequeue(GameObject obj, Action<int, GameObject, object[]> action = null, int cacheCount = 10, params object[] param)
    {
        if (obj == null) return -1;
        var id = obj.GetInstanceID();
        GameObjectPool<ObjectPoolItem> temp = null;
        if (!pool.TryGetValue(id, out temp))
        {
            temp = new GameObjectPool<ObjectPoolItem>();
            temp.Init(obj, cacheCount);
            pool.Add(id, temp);
        }
        return temp.Dequeue(action, param).ItemID;
    }
    public void Destroy()
    {
        foreach (var item in pool) item.Value.Destroy();
        pool.Clear();
    }
}
public class GameObjectPool<T> where T : ObjectPoolItem, new()
{
    private GameObject obj;
    private List<T> use = new();
    private List<T> cache = new();
    private int cacheCount = 10;

    public List<T> Use => use;

    public void Init(GameObject obj, int cacheCount = 10)
    {
        this.obj = obj;
        this.cacheCount = cacheCount;
    }
    public void Enqueue(int itemId)
    {
        T temp = use.Find(a => a.ItemID == itemId);
        if (temp == null) return;
        use.Remove(temp);
        if (cache.Count >= cacheCount) temp.Destroy();
        else { temp.Disable(); cache.Add(temp); }
    }
    public T Dequeue(Action<int, GameObject, object[]> action = null, params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(Delete, action, param);
        use.Add(temp);
        temp.Enable(obj);
        return temp;
    }
    public void Destroy()
    {
        for (int i = use.Count - 1; i >= 0; i--) use[i].Destroy();
        for (int i = cache.Count - 1; i >= 0; i--) cache[i].Destroy();
        use.Clear();
        cache.Clear();
    }
    private void Delete(int itemId)
    {
        int index = use.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            use.RemoveAt(index);
            return;
        }
        index = cache.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            cache.RemoveAt(index);
            return;
        }
    }
}
public class AssetObjectPool
{
    private Dictionary<string, AssetObjectPool<ObjectPoolItem>> pool = new();
    public void Enqueue(string path, int itemId)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (pool.TryGetValue(path, out var temp)) temp.Enqueue(itemId);
    }
    public int Dequeue(string path, Action<int, GameObject, object[]> action = null, int cacheCount = 10, params object[] param)
    {
        if (string.IsNullOrEmpty(path)) return -1;
        if (!pool.TryGetValue(path, out var temp))
        {
            temp = new AssetObjectPool<ObjectPoolItem>();
            temp.Init(path, cacheCount);
            pool.Add(path, temp);
        }
        return temp.Dequeue(action, param).ItemID;
    }
    public void Destroy()
    {
        foreach (var item in pool) item.Value.Destroy();
        pool.Clear();
    }
}
public class AssetObjectPool<T> where T : ObjectPoolItem, new()
{
    private string path;
    private int loadId = -1;
    private Object asset;
    private List<T> use = new();
    private List<T> wait = new();
    private List<T> cache = new();
    private int cacheCount = 10;

    public List<T> Use => use;

    public void Init(string path, int cacheCount = 10)
    {
        this.path = path;
        this.cacheCount = cacheCount;
        AssetManager.Instance.Load<GameObject>(ref loadId, path, LoadFinish);
    }
    private void LoadFinish(int loadId, Object asset)
    {
        if (asset == null)
        {
            //加载失败一直加载不回调，理论上不应该出现加载失败
            AssetManager.Instance.Load<GameObject>(ref this.loadId, path, LoadFinish);
        }
        else
        {
            this.asset = asset;
            for (int i = 0; i < wait.Count; i++) wait[i].Enable(asset);
            wait.Clear();
        }
    }

    public void Enqueue(int itemId)
    {
        T temp = use.Find(a => a.ItemID == itemId);
        if (temp == null) return;
        use.Remove(temp);
        wait.Remove(temp);
        if (cache.Count >= cacheCount) temp.Destroy();
        else { temp.Disable(); cache.Add(temp); }
    }
    public T Dequeue(Action<int, GameObject, object[]> action = null, params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(Delete, action, param);
        use.Add(temp);
        if (asset == null) wait.Add(temp);
        else temp.Enable(asset);
        return temp;
    }
    public void Destroy()
    {
        //先卸载，使Delete调用时Unload(-1)
        AssetManager.Instance.Unload(ref loadId);
        for (int i = use.Count - 1; i >= 0; i--) use[i].Destroy();
        for (int i = cache.Count - 1; i >= 0; i--) cache[i].Destroy();
        use.Clear();
        wait.Clear();
        cache.Clear();
    }
    private void Delete(int itemId)
    {
        int index = use.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            use.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) AssetManager.Instance.Unload(ref loadId, CacheTime.Short);
            return;
        }
        index = cache.FindIndex(a => a.ItemID == itemId);
        if (index >= 0)
        {
            cache.RemoveAt(index);
            if (use.Count == 0 && wait.Count == 0 && cache.Count == 0) AssetManager.Instance.Unload(ref loadId, CacheTime.Short);
            return;
        }
    }
}
public class ObjectPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
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
    public void Init(Action<int> release, Action<int, GameObject, object[]> action, params object[] param)
    {
        itemId = ++uniqueId;
        this.release = release;
        this.action = action;
        this.param = param;
    }
    [Obsolete("请使用GameObjectPool")]
    public void Enable(Object _asset)
    {
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
        if (_asset == null)
        {
            Destroy();
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
        if (obj != null)
        {
            obj.SetActive(true);
        }
        if (action != null)
        {
            action?.Invoke(itemId, obj, param);
        }
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Disable()
    {
        obj?.SetActive(false);
        if (timerId < 0) timerId = Driver.Instance.StartTimer(GameSetting.recycleTimeS, finish: Destroy);
        state |= LoadState.Release;
        action = null;
        param = null;
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Destroy()
    {
        asset = null;
        aio = null;
        if (obj != null) GameObject.Destroy(obj);
        obj = null;
        state = LoadState.None;
        Driver.Instance.StopTimer(timerId);
        timerId = -1;
        release?.Invoke(itemId);
        release = null;
        itemId = -1;
        action = null;
        param = null;
    }
    private void Recycle()
    {
        state &= LoadState.InstantiateFinish | LoadState.Instantiating;
        Driver.Instance.StopTimer(timerId);
        timerId = -1;
    }
}