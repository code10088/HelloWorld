using System;
using System.Collections.Generic;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

public class AssetManager : Singletion<AssetManager>
{
    private static Dictionary<int, AssetItemGroup> group = new();
    private static Dictionary<int, AssetItem> total = new();
    private static Queue<AssetItem> cache = new();
    private static int uniqueId = 0;
    private Action initFinish;

    public void Init(Action action)
    {
        initFinish = action;
        Assets.InitializeAsync(InitFinish);
    }
    private void InitFinish(Request completed)
    {
        initFinish?.Invoke();
    }

    public int Load<T>(string path, Action<int, Object> action = null) where T : Object
    {
        AssetItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init<T>(path, action);
        total[temp.ItemID] = temp;
        return temp.ItemID;
    }
    public int Load(string[] path, Action<string[], Object[]> action = null)
    {
        AssetItemGroup temp = new();
        temp.Init(path, action);
        group[temp.ItemID] = temp;
        return temp.ItemID;
    }
    public void Unload(int id)
    {
        if (group.TryGetValue(id, out AssetItemGroup a))
        {
            a.Unload();
            group.Remove(id);
        }
        else if (total.TryGetValue(id, out AssetItem b))
        {
            b.Unload();
            total.Remove(id);
        }
    }
    public float GetProgerss(int id)
    {
        float progress = 0;
        if (group.TryGetValue(id, out AssetItemGroup a)) progress = a.Progress;
        else if (total.TryGetValue(id, out AssetItem b)) progress = b.Progress;
        return progress;
    }


    private class AssetItemGroup
    {
        private int itemId = -1;
        private Action<string[], Object[]> action;
        private string[] path;
        private int[] ids;
        private Object[] assets;
        private int complete;
        public int ItemID => itemId;
        public float Progress => (float)complete / ids.Length;

        public void Init(string[] path, Action<string[], Object[]> action)
        {
            itemId = ++uniqueId;
            this.path = path;
            this.action = action;
            ids = new int[path.Length];
            assets = new Object[path.Length];
            for (int i = 0; i < path.Length; i++) ids[i] = Instance.Load<Object>(path[i], Finish);
        }
        private void Finish(int id, Object asset)
        {
            int index = Array.FindIndex(ids, a => a == id);
            assets[index] = asset;
            if (++complete == ids.Length) action?.Invoke(path, assets);
        }
        public void Unload()
        {
            for (int i = 0; i < ids.Length; i++) Instance.Unload(ids[i]);
            action = null;
            path = null;
            ids = null;
            assets = null;
        }
    }
    private class AssetItem
    {
        private int itemId = -1;
        private Action<int, Object> action;
        private AssetRequest ar;
        public int ItemID => itemId;
        public float Progress => ar == null ? 0 : ar.progress;

        public void Init<T>(string path, Action<int, Object> action) where T : Object
        {
            itemId = ++uniqueId;
            this.action = action;
            ar = Asset.LoadAsync(path, typeof(T));
            if (ar == null) Finish(null);
            else ar.completed += Finish;
        }
        private void Finish(Request request)
        {
            Object asset = ar == null ? null : ar.asset;
            action?.Invoke(itemId, asset);
        }
        public void Unload()
        {
            cache.Enqueue(this);
            if (ar == null) return;
            ar.completed -= Finish;
            ar.Release();
            ar = null;
            action = null;
        }
    }
}
public class LoadGameObjectItem
{
    protected string path;
    protected Transform parent;
    protected Object asset;
    protected GameObject obj;
    private int loaderID;
    private int state = 4;//7：二进制111：分别表示release instantiate load
    private int timer = 0;
    private int timerId = -1;

    public void Init(string path, Transform parent)
    {
        this.path = path;
        this.parent = parent;
    }
    public void SetActive(bool b)
    {
        if (b && state > 3)
        {
            Recycle();
            Load();
        }
        else if (!b && state <= 3)
        {
            Delay();
        }
    }
    private void Load()
    {
        if (state == 0)
        {
            loaderID = AssetManager.Instance.Load<GameObject>(path, LoadFinish);
        }
        else if (state == 1)
        {
            LoadFinish(loaderID, asset);
        }
        else
        {
            obj.SetActive(true);
            Finish(1, obj);
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Release();
            Finish(-1, null);
        }
        else if (state > 3)
        {
            asset = _asset;
            state |= 1;
            Finish(-1, null);
        }
        else
        {
            state = 3;
            obj = Object.Instantiate(_asset, Vector3.zero, Quaternion.identity, parent) as GameObject;
            Finish(0, obj);
        }
    }
    /// <summary>
    /// state=-1：未加载成功或加载过程中卸载
    /// state=0：第一次成功加载
    /// state=1：缓存
    /// </summary>
    protected virtual void Finish(int state, GameObject obj)
    {

    }
    private void Delay()
    {
        obj?.SetActive(false);
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(timer, finish: Release);
        state |= 4;
    }
    public virtual void Release()
    {
        TimeManager.Instance.StopTimer(timerId);
        if (obj != null) GameObject.Destroy(obj);
        AssetManager.Instance.Unload(loaderID);
        parent = null;
        asset = null;
        obj = null;
        loaderID = -1;
        state = 4;
        timer = 0;
        timerId = -1;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timer += GameSetting.recycleTime;
        timerId = -1;
        state &= 3;
    }
}
public class LoadAssetItem
{
    protected string path;
    protected Object asset;
    private int loaderID;
    private bool releaseMark = true;
    private bool loadMark = false;
    private int timer = 0;
    private int timerId = -1;

    public void Init(string path)
    {
        this.path = path;
    }
    public void Load<T>() where T : Object
    {
        if (releaseMark)
        {
            Recycle();
        }
        if (loadMark)
        {
            Finish(asset);
        }
        else
        {
            loaderID = AssetManager.Instance.Load<T>(path, LoadFinish);
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else
        {
            asset = _asset;
            loadMark = true;
            if (releaseMark) Finish(null);
            else Finish(asset);
        }
    }
    /// <summary>
    /// state=-1：未加载成功或加载过程中卸载
    /// state=0：第一次成功加载
    /// state=1：缓存
    /// </summary>
    protected virtual void Finish(Object asset)
    {

    }
    public void Delay()
    {
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(timer, finish: Release);
        releaseMark = true;
    }
    public virtual void Release()
    {
        TimeManager.Instance.StopTimer(timerId);
        AssetManager.Instance.Unload(loaderID);
        asset = null;
        loaderID = -1;
        releaseMark = true;
        loadMark = false;
        timer = 0;
        timerId = -1;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timer += GameSetting.recycleTime;
        timerId = -1;
        releaseMark = false;
    }
}