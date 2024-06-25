using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameObjectPool
{
    private Dictionary<string, GameObjectPool<GameObjectPoolItem>> pool = new();
    public void Enqueue(string path, int itemId)
    {
        if (pool.TryGetValue(path, out var temp)) temp.Enqueue(itemId);
    }
    public GameObjectPoolItem Dequeue(string path, Transform parent, Action<int, GameObject, object[]> action = null, params object[] param)
    {
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
        pool = null;
    }
}
public class GameObjectPool<T> : LoadAssetItem where T : GameObjectPoolItem, new()
{
    private List<T> use = new();
    private List<T> wait = new();
    private List<T> cache = new();
    private int cacheCount = 10;
    private int frameId = -1;

    public List<T> Use => use;
    public int CacheCount => cacheCount;

    public void Enqueue(int itemId)
    {
        T temp = use.Find(a => a.ItemID == itemId);
        if (temp == null) return;
        use.Remove(temp);
        wait.Remove(temp);
        if (cache.Count >= cacheCount) temp.Release();
        else { temp.SetActive(false); cache.Add(temp); }
    }
    public T Dequeue(Transform parent, Action<int, GameObject, object[]> action = null, params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(parent, Delete, action, param);
        temp.SetActive(true);
        use.Add(temp);
        wait.Add(temp);
        Load<GameObject>();
        return temp;
    }
    public override void Release()
    {
        for (int i = use.Count - 1; i >= 0; i--) use[i].Release();
        for (int i = cache.Count - 1; i >= 0; i--) cache[i].Release();
        FrameManager.Instance.StopFrame(frameId);
        use.Clear();
        wait.Clear();
        cache.Clear();
        frameId = -1;
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
        if (asset == null) return;
        if (frameId < 0) frameId = FrameManager.Instance.StartFrame(0, 1, Instantiate);
    }
    private void Instantiate(int frame)
    {
        if (wait.Count == 0)
        {
            FrameManager.Instance.StopFrame(frameId);
            frameId = -1;
        }
        else
        {
            wait[0].Instantiate(asset);
            wait.RemoveAt(0);
        }
    }
}
public class GameObjectPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    private Transform parent;
    protected GameObject obj;
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
    public void SetActive(bool b)
    {
        if (b) Recycle();
        else Delay();
    }
    protected virtual void Delay()
    {
        if (obj == null)
        {
            Release();
        }
        else
        {
            obj.SetActive(false);
            if (timerId < 0) timerId = TimeManager.Instance.StartTimer(GameSetting.recycleTimeS, finish: Release);
            param = null;
        }
    }
    [Obsolete("请使用GameObjectPool")]
    public virtual void Release()
    {
        TimeManager.Instance.StopTimer(timerId);
        if (obj != null) GameObject.Destroy(obj);
        release?.Invoke(itemId);
        itemId = -1;
        obj = null;
        timerId = -1;
        param = null;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
        if (obj != null) obj.SetActive(true);
    }
    [Obsolete("请使用GameObjectPool")]
    public void Instantiate(Object asset)
    {
        if (obj == null) obj = Object.Instantiate(asset, parent) as GameObject;
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.SetActive(true);
        LoadFinish();
    }
    protected virtual void LoadFinish()
    {
        action?.Invoke(itemId, obj, param);
    }
}


public class AssetPool
{
    private Dictionary<string, AssetPool<AssetPoolItem>> pool = new();
    public void Enqueue(string path, int itemId)
    {
        if (pool.TryGetValue(path, out var temp)) temp.Enqueue(itemId);
    }
    public AssetPoolItem Dequeue(string path, Action<int, Object, object[]> action = null, params object[] param)
    {
        AssetPool<AssetPoolItem> temp = null;
        if (!pool.TryGetValue(path, out temp))
        {
            temp = new AssetPool<AssetPoolItem>();
            temp.Init(path);
            pool.Add(path, temp);
        }
        return temp.Dequeue(action, param);
    }
    public void Release()
    {
        foreach (var item in pool) item.Value.Release();
        pool = null;
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
        else { temp.SetActive(false); cache.Add(temp); }
    }
    public T Dequeue(Action<int, Object, object[]> action = null, params object[] param)
    {
        T temp = null;
        if (cache.Count > 0) temp = cache[0];
        if (temp != null) cache.RemoveAt(0);
        else temp = new T();
        temp.Init(Delete, action, param);
        temp.SetActive(true);
        use.Add(temp);
        wait.Add(temp);
        Load<Object>();
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
        if (asset == null) return;
        for (int i = 0; i < wait.Count; i++) wait[i].Instantiate(asset);
        wait.Clear();
    }
}
public class AssetPoolItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    protected Object obj;
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
    public void SetActive(bool b)
    {
        if (b) Recycle();
        else Delay();
    }
    protected virtual void Delay()
    {
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(GameSetting.recycleTimeS, finish: Release);
        param = null;
    }
    [Obsolete("请使用AssetPool")]
    public virtual void Release()
    {
        TimeManager.Instance.StopTimer(timerId);
        release?.Invoke(itemId);
        itemId = -1;
        obj = null;
        timerId = -1;
        param = null;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
    [Obsolete("请使用AssetPool")]
    public void Instantiate(Object asset)
    {
        obj = asset;
        LoadFinish();
    }
    public virtual void LoadFinish()
    {
        action?.Invoke(itemId, obj, param);
    }
}