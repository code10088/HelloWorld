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

    public const string PackageName = "All";
    private static ResourcePackage package;
    private Action initFinish;

    public static ResourcePackage Package => package;

    public void Init(Action action)
    {
        initFinish = action;
        YooAssets.Initialize();
        package = YooAssets.CreatePackage(PackageName);
#if UNITY_EDITOR && !HotUpdateDebug
        var simulate = EditorSimulateModeHelper.SimulateBuild(PackageName);
        var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulate.PackageRootDirectory);
        var parameters = new EditorSimulateModeParameters();
        parameters.EditorFileSystemParameters = editorFileSystem;
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#elif UNITY_WEBGL
        string defaultHostServer = GameSetting.Instance.CDNVersion;
        string fallbackHostServer = GameSetting.Instance.CDNVersion;
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        WebDecryptionServices decryptionServices = new WebDecryptionServices();
        var parameters = new WebPlayModeParameters();
#if WEIXINMINIGAME
        parameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateWechatFileSystemParameters(Application.version, remoteServices, decryptionServices);
#elif DOUYINMINIGAME
        parameters.WebServerFileSystemParameters = TiktokFileSystemCreater.CreateByteGameFileSystemParameters(Application.version, remoteServices, decryptionServices);
#else
        parameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(decryptionServices);
        parameters.WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices, decryptionServices);
#endif
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#else
        string defaultHostServer = GameSetting.Instance.CDNVersion;
        string fallbackHostServer = GameSetting.Instance.CDNVersion;
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        DecryptionServices decryptionServices = new DecryptionServices();
        var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices);   
        var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices, decryptionServices);
        var parameters = new HostPlayModeParameters();
        parameters.BuildinFileSystemParameters = buildinFileSystem;
        parameters.CacheFileSystemParameters = cacheFileSystem;
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
        if (loadId > 0) Unload(ref loadId);
        AssetItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init<T>(path, action);
        total[temp.ItemID] = temp;
        loadId = temp.ItemID;
    }
    public void Load(ref int loadId, string[] path, Action<string[], Object[]> action = null)
    {
        if (loadId > 0) Unload(ref loadId);
        AssetItemGroup temp = new();
        temp.Init(path, action);
        group[temp.ItemID] = temp;
        loadId = temp.ItemID;
    }
    public void Unload(ref int id)
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
        id = -1;
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
            for (int i = 0; i < ids.Length; i++) Instance.Unload(ref ids[i]);
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
            ah.Completed += Finish;
        }
        private void Finish(AssetHandle _ah)
        {
            ah.Completed -= Finish;
            action?.Invoke(itemId, ah.AssetObject);
        }
        public void Unload()
        {
            cache.Enqueue(this);
            ah.Release();
            var asset = ah.GetAssetInfo();
            package.TryUnloadUnusedAsset(asset);
            ah = null;
            action = null;
        }
    }
}
public enum LoadState
{
    None = 0,
    Loading = 1,
    LoadFinish = 2,
    Instantiating = 4,
    InstantiateFinish = 8,
    Release = 16,
}
/// <summary>
/// 父节点删除时必须Release，否则加载状态会错乱(虽有办法避免，但不卸载导致的资源常驻无法避免)
/// </summary>
public class LoadGameObjectItem
{
    protected string path;
    protected Transform parent;
    protected Object asset;
    private AsyncInstantiateOperation<Object> aio;
    protected GameObject obj;
    private int loadId;
    private LoadState state = LoadState.Release;
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
        if (b && state.HasFlag(LoadState.Release))
        {
            Recycle();
            Load();
        }
        else if (!b && !state.HasFlag(LoadState.Release))
        {
            Delay();
        }
    }
    private void Load()
    {
        switch (state)
        {
            case LoadState.None:
                state = LoadState.Loading;
                AssetManager.Instance.Load<GameObject>(ref loadId, path, LoadFinish);
                break;
            case LoadState.Loading:
                break;
            case LoadState.LoadFinish:
                LoadFinish(loadId, asset);
                break;
            case LoadState.Instantiating:
                break;
            case LoadState.InstantiateFinish:
                obj.SetActive(true);
                Finish(obj);
                break;
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else if (state.HasFlag(LoadState.Release))
        {
            asset = _asset;
            state = LoadState.LoadFinish | LoadState.Release;
            Finish(null);
        }
        else
        {
            asset = _asset;
            state = LoadState.Instantiating;
            aio = Object.InstantiateAsync(_asset);
            aio.completed += InstantiateFinish;
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
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(false);
            Finish(null);
        }
        else
        {
            obj = aio.Result[0] as GameObject;
            state = LoadState.InstantiateFinish;
            obj.transform.SetParent(parent);
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
        state |= LoadState.Release;
    }
    public virtual void Release()
    {
        parent = null;
        asset = null;
        aio = null;
        if (obj != null) GameObject.Destroy(obj);
        obj = null;
        AssetManager.Instance.Unload(ref loadId);
        state = LoadState.Release;
        timer = 0;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
    private void Recycle()
    {
        state &= LoadState.InstantiateFinish | LoadState.Instantiating | LoadState.LoadFinish | LoadState.Loading;
        timer += GameSetting.recycleTimeS;
        timer = Math.Min(timer, GameSetting.recycleTimeMaxS);
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
}
public class LoadAssetItem
{
    protected string path;
    protected Object asset;
    private int loadId;
    private LoadState state = LoadState.Release;
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
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
        switch (state)
        {
            case LoadState.None:
                state = LoadState.Loading;
                AssetManager.Instance.Load<T>(ref loadId, path, LoadFinish);
                break;
            case LoadState.Loading:
                break;
            case LoadState.LoadFinish:
                Finish(asset);
                break;
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Release();
            Finish(null);
        }
        else if (state.HasFlag(LoadState.Release))
        {
            asset = _asset;
            state = LoadState.LoadFinish | LoadState.Release;
            Finish(null);
        }
        else
        {
            asset = _asset;
            state = LoadState.LoadFinish;
            Finish(asset);
        }
    }
    protected virtual void Finish(Object asset)
    {
        action?.Invoke(asset, param);
    }
    public void Delay()
    {
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(timer, finish: Release);
        state |= LoadState.Release;
    }
    public virtual void Release()
    {
        asset = null;
        AssetManager.Instance.Unload(ref loadId);
        state = LoadState.Release;
        timer = 0;
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
    private void Recycle()
    {
        state &= LoadState.LoadFinish | LoadState.Loading;
        timer += GameSetting.recycleTimeS;
        timer = Math.Min(timer, GameSetting.recycleTimeMaxS);
        TimeManager.Instance.StopTimer(timerId);
        timerId = -1;
    }
}