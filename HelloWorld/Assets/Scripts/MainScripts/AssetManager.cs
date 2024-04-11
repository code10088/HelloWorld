using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

public class AssetManager : Singletion<AssetManager>
{
    private static Dictionary<int, AssetItemGroup> group = new();
    private static Dictionary<int, AssetItem> total = new();
    private static Queue<AssetItem> cache = new();
    private static int uniqueId = 0;

    private static ResourcePackage package;
    private Action initFinish;

    public const string PackageName = "All";
    public static ResourcePackage Package => package;

    public void Init(Action action)
    {
        initFinish = action;
        YooAssets.Initialize();
        package = YooAssets.CreatePackage(PackageName);
#if UNITY_EDITOR && !HotUpdateDebug
        var parameters = new EditorSimulateModeParameters();
        var path = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, PackageName);
        parameters.CacheBootVerifyLevel = EVerifyLevel.Middle;
        parameters.AutoDestroyAssetProvider = true;
        parameters.BreakpointResumeFileSize = 102400;
        parameters.SimulateManifestFilePath = path;
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#elif UNITY_WEBGL
        string defaultHostServer = GameSetting.Instance.CDNVersion;
        string fallbackHostServer = GameSetting.Instance.CDNVersion;
        var parameters = new WebPlayModeParameters();
        parameters.CacheBootVerifyLevel = EVerifyLevel.Middle;
        parameters.AutoDestroyAssetProvider = true;
        parameters.BreakpointResumeFileSize = 102400;
        parameters.BuildinQueryServices = new QueryServices();
        parameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#else
        string defaultHostServer = GameSetting.Instance.CDNVersion;
        string fallbackHostServer = GameSetting.Instance.CDNVersion;
        var parameters = new HostPlayModeParameters();
        parameters.CacheBootVerifyLevel = EVerifyLevel.Middle;
        parameters.AutoDestroyAssetProvider = true;
        parameters.BreakpointResumeFileSize = 102400;
        parameters.BuildinQueryServices = new QueryServices();
        parameters.DecryptionServices = new DecryptionServices();
        parameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#endif
    }
    private void InitFinish(AsyncOperationBase operation)
    {
        initFinish?.Invoke();
    }

    public void Load<T>(ref int loadId, string path, Action<int, Object> action = null) where T : Object
    {
        if (loadId > 0) Unload(loadId);
        AssetItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init<T>(path, action);
        total[temp.ItemID] = temp;
        loadId = temp.ItemID;
    }
    public void Load(ref int loadId, string[] path, Action<string[], Object[]> action = null)
    {
        if (loadId > 0) Unload(loadId);
        AssetItemGroup temp = new();
        temp.Init(path, action);
        group[temp.ItemID] = temp;
        loadId = temp.ItemID;
    }
    public void Unload(int id)
    {
        if (id < 0) return;
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
            for (int i = 0; i < path.Length; i++) Instance.Load<Object>(ref ids[i], path[i], Finish);
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
        private AssetHandle ah;
        public int ItemID => itemId;
        public float Progress => ah == null ? 0 : ah.Progress;

        public void Init<T>(string path, Action<int, Object> action) where T : Object
        {
            itemId = ++uniqueId;
            this.action = action;
            ah = package.LoadAssetAsync<T>(path);
            if (ah == null) Finish(null);
            else ah.Completed += Finish;
        }
        private void Finish(AssetHandle _ah)
        {
            Object asset = ah == null ? null : ah.AssetObject;
            action?.Invoke(itemId, asset);
        }
        public void Unload()
        {
            cache.Enqueue(this);
            if (ah == null) return;
            ah.Completed -= Finish;
            ah.Release();
            ah = null;
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
    private int loadId;
    private int state = 4;//7：二进制111：分别表示release instantiate load
    private float timer = 0;
    private int timerId = -1;

    private Action<GameObject, object[]> action;
    protected object[] param;

    public void Init(string path, Transform parent, Action<GameObject, object[]> action = null, params object[] param)
    {
        this.path = path;
        this.parent = parent;
        this.action = action;
        this.param = param;
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
            AssetManager.Instance.Load<GameObject>(ref loadId, path, LoadFinish);
        }
        else if (state == 1)
        {
            LoadFinish(loadId, asset);
        }
        else
        {
            obj.SetActive(true);
            Finish(obj);
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else if (state > 3)
        {
            asset = _asset;
            state |= 1;
            Finish(null);
        }
        else
        {
            state = 3;
            obj = Object.Instantiate(_asset, parent) as GameObject;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            Finish(obj);
        }
    }
    protected virtual void Finish(GameObject obj)
    {
        action?.Invoke(obj, param);
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
        AssetManager.Instance.Unload(loadId);
        parent = null;
        asset = null;
        obj = null;
        loadId = -1;
        state = 4;
        timer = 0;
        timerId = -1;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timer += GameSetting.recycleTimeS;
        timer = Math.Min(timer, GameSetting.recycleTimeMaxS);
        timerId = -1;
        state &= 3;
    }
}
public class LoadAssetItem
{
    protected string path;
    protected Object asset;
    private int loadId;
    private bool releaseMark = true;
    private bool loadMark = false;
    private float timer = 0;
    private int timerId = -1;

    private Action<Object, object[]> action;
    protected object[] param;

    public void Init(string path, Action<Object, object[]> action = null, params object[] param)
    {
        this.path = path;
        this.action = action;
        this.param = param;
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
            AssetManager.Instance.Load<T>(ref loadId, path, LoadFinish);
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
    protected virtual void Finish(Object asset)
    {
        action?.Invoke(asset, param);
    }
    public void Delay()
    {
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(timer, finish: Release);
        releaseMark = true;
    }
    public virtual void Release()
    {
        TimeManager.Instance.StopTimer(timerId);
        AssetManager.Instance.Unload(loadId);
        asset = null;
        loadId = -1;
        releaseMark = true;
        loadMark = false;
        timer = 0;
        timerId = -1;
    }
    private void Recycle()
    {
        TimeManager.Instance.StopTimer(timerId);
        timer += GameSetting.recycleTimeS;
        timer = Math.Min(timer, GameSetting.recycleTimeMaxS);
        timerId = -1;
        releaseMark = false;
    }
}