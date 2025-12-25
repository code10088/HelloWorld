using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset;
using Object = UnityEngine.Object;

public enum LoadState
{
    None = 0,
    Loading = 1,
    LoadFinish = 2,
    Instantiating = 4,
    InstantiateFinish = 8,
    Release = 16,
}
public enum CacheTime
{
    None,
    Short,
    Long,
}
public class AssetManager : Singletion<AssetManager>
{
    private Dictionary<int, AssetItemGroup> group = new();
    private Dictionary<int, string> totalKey = new();
    private Dictionary<string, AssetItem> totalValue = new();
    private Queue<AssetItem> cache = new();
    private int uniqueId = 0;

    public const string PackageName = "All";
    private ResourcePackage package;
    private Action initFinish;

    public ResourcePackage Package => package;

    public void Init(Action action)
    {
        initFinish = action;
        YooAssets.Initialize();
        package = YooAssets.CreatePackage(PackageName);
#if UNITY_EDITOR && !HotUpdateDebug
        var simulate = EditorSimulateModeHelper.SimulateBuild(PackageName);
        var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulate.PackageRootDirectory);
        var parameters = new EditorSimulateModeParameters();
        parameters.AutoUnloadBundleWhenUnused = true;
        parameters.EditorFileSystemParameters = editorFileSystem;
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#elif UNITY_WEBGL
        string defaultHostServer = GameSetting.CDNVersion;
        string fallbackHostServer = GameSetting.CDNVersion;
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        WebDecryptionServices decryptionServices = new WebDecryptionServices();
        var parameters = new WebPlayModeParameters();
        parameters.AutoUnloadBundleWhenUnused = true;
#if WEIXINMINIGAME
        string packageRoot = WeChatWASM.WX.PluginCachePath + "/" + Application.version;
        parameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, decryptionServices);
#elif DOUYINMINIGAME
        string packageRoot = Application.version;
        parameters.WebServerFileSystemParameters = TiktokFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, decryptionServices);
#else
        parameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(decryptionServices);
        parameters.WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices, decryptionServices);
#endif
        var operation = package.InitializeAsync(parameters);
        operation.Completed += InitFinish;
#else
        string defaultHostServer = GameSetting.CDNVersion;
        string fallbackHostServer = GameSetting.CDNVersion;
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        DecryptionServices decryptionServices = new DecryptionServices();
        var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices);   
        var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices, decryptionServices);
        cacheFileSystem.AddParameter(FileSystemParametersDefine.DOWNLOAD_WATCH_DOG_TIME, GameSetting.timeoutS);
        var parameters = new HostPlayModeParameters();
        parameters.AutoUnloadBundleWhenUnused = true;
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
        if (!totalValue.TryGetValue(path, out AssetItem temp))
        {
            temp = cache.Count > 0 ? cache.Dequeue() : new();
            temp.Init<T>(++uniqueId, path);
            totalKey.Add(uniqueId, path);
            totalValue.Add(path, temp);
        }
        temp.Load(ref loadId, action);
    }
    public void Load(ref int loadId, string[] path, Action<string[], Object[]> action = null)
    {
        if (loadId > 0) Unload(ref loadId);
        AssetItemGroup temp = new();
        temp.Init(++uniqueId, path);
        group.Add(uniqueId, temp);
        temp.Load(ref loadId, action);
    }
    public void Unload(ref int id, CacheTime time = CacheTime.None)
    {
        if (id < 0) return;
        var temp = id >> 8;
        if (group.TryGetValue(temp, out AssetItemGroup a)) a.Unload(temp, time);
        else if (totalKey.TryGetValue(temp, out string b)) totalValue[b].Unload(id, time);
        id = -1;
    }
    public float GetProgerss(int id)
    {
        float progress = 0;
        var temp = id >> 8;
        if (group.TryGetValue(temp, out AssetItemGroup a)) progress = a.Progress;
        else if (totalKey.TryGetValue(temp, out string b)) progress = totalValue[b].Progress;
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
        public float Progress => (float)complete / ids.Length;

        public void Init(int itemId, string[] path)
        {
            this.path = path;
            this.itemId = itemId << 8;
        }
        public void Load(ref int loadId, Action<string[], Object[]> action)
        {
            loadId = itemId;
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
        public void Unload(int id, CacheTime time)
        {
            Instance.group.Remove(id);
            for (int i = 0; i < ids.Length; i++) Instance.Unload(ref ids[i], time);
            action = null;
            path = null;
            ids = null;
            assets = null;
        }
    }
    private class AssetItem
    {
        private string path;
        private int itemId = -1;
        private int loadId = 0;
        private LoadState state = LoadState.None;
        private AssetHandle ah;
        private Dictionary<int, Action<int, Object>> loaders = new();
        private CacheTime timer = CacheTime.None;
        private int timerId = -1;
        public float Progress => ah == null ? 0 : ah.Progress;

        public void Init<T>(int itemId, string path) where T : Object
        {
            this.path = path;
            this.itemId = itemId;
            state = LoadState.Loading;
            ah = Instance.package.LoadAssetAsync<T>(path);
            ah.Completed += LoadFinish;
        }
        public void Load(ref int loadId, Action<int, Object> action)
        {
            this.loadId = this.loadId + 1 & 0xFF;
            loadId = itemId << 8 | this.loadId;
            if (state.HasFlag(LoadState.Release))
            {
                state &= LoadState.LoadFinish | LoadState.Loading;
                Driver.Instance.Remove(timerId);
                timerId = -1;
            }
            switch (state)
            {
                case LoadState.Loading:
                    loaders.Add(loadId, action);
                    break;
                case LoadState.LoadFinish:
                    action?.Invoke(loadId, ah.AssetObject);
                    break;
            }
        }
        private void LoadFinish(AssetHandle _ah)
        {
            ah.Completed -= LoadFinish;
            if (ah.AssetObject == null)
            {
                //防止回调时加载卸载
                Instance.totalKey.Remove(itemId);
                Instance.totalValue.Remove(path);
                foreach (var item in loaders) item.Value?.Invoke(item.Key, null);
                Unload();
            }
            else
            {
                if (state.HasFlag(LoadState.Release)) state = LoadState.LoadFinish | LoadState.Release;
                else state = LoadState.LoadFinish;

                //注意：A的回调中卸载B，可能导致B的回调无法执行
                var list = loaders.Keys.ToList();
                foreach (var item in list)
                {
                    if (loaders.TryGetValue(item, out var temp) && temp != null)
                    {
                        //先置空再回调，否则回调时Unload然后loaders[item] = null又会加到loaders中
                        loaders[item] = null;
                        temp.Invoke(item, ah.AssetObject);
                    }
                }
            }
        }
        public void Unload(int id, CacheTime time)
        {
            var b = loaders.Remove(id);
            if (b && timer < time) timer = time;
            if (!b || loaders.Count > 0) return;
            switch (timer)
            {
                case CacheTime.None:
                    Unload();
                    break;
                case CacheTime.Short:
                    if (timerId < 0) timerId = Driver.Instance.StartTimer(GameSetting.recycleTimeS, finish: Unload);
                    state |= LoadState.Release;
                    break;
                case CacheTime.Long:
                    if (timerId < 0) timerId = Driver.Instance.StartTimer(GameSetting.recycleTimeMaxS, finish: Unload);
                    state |= LoadState.Release;
                    break;
            }
        }
        private void Unload()
        {
            Instance.totalKey.Remove(itemId);
            Instance.totalValue.Remove(path);
            itemId = -1;
            loadId = 0;
            state = LoadState.None;
            ah.Release();
            var asset = ah.GetAssetInfo();
            Instance.package.TryUnloadUnusedAsset(asset);
            ah = null;
            loaders.Clear();
            timer = CacheTime.None;
            Driver.Instance.Remove(timerId);
            timerId = -1;
            Instance.cache.Enqueue(this);
        }
    }
}
public class AtlasManager : Singletion<AtlasManager>
{
    private Dictionary<string, AtlasInfo> atlasInfos = new();
    private Dictionary<int, List<string>> atlasRefs = new();
    private int uniqueId = 0;

    public void LoadSprite(ref int id, string atlas, string name, Action<Sprite> action)
    {
        bool addref = true;
        if (id > 0)
        {
            if (atlasRefs[id].Contains(atlas)) addref = false;
            else atlasRefs[id].Add(atlas);
        }
        else
        {
            id = ++uniqueId;
            atlasRefs.Add(id, new List<string> { atlas });
        }
        if (atlasInfos.TryGetValue(atlas, out var info))
        {
            if (addref) info.AddRef();
            info.LoadSprite(name, action);
        }
        else
        {
            info = new AtlasInfo(atlas);
            atlasInfos.Add(atlas, info);
            if (addref) info.AddRef();
            info.LoadSprite(name, action);
        }
    }
    public void UnloadSprite(ref int id)
    {
        if (id < 0) return;
        if (atlasRefs.TryGetValue(id, out var list))
        {
            foreach (var atlas in list)
            {
                if (atlasInfos[atlas].RemoveRef()) atlasInfos.Remove(atlas);
            }
            atlasRefs.Remove(id);
            id = -1;
        }
    }


    private class AtlasInfo
    {
        private int loadId;
        private SpriteAtlas atlas;
        private Dictionary<string, Sprite> sprites;
        private Dictionary<string, List<Action<Sprite>>> callback;
        private int refCount = 0;

        public AtlasInfo(string path)
        {
            sprites = new Dictionary<string, Sprite>();
            callback = new Dictionary<string, List<Action<Sprite>>>();
            AssetManager.Instance.Load<SpriteAtlas>(ref loadId, path, LoadFinish);
        }
        private void LoadFinish(int loadId, Object asset)
        {
            atlas = asset as SpriteAtlas;
            foreach (var item in callback)
            {
                var sprite = GetSprite(item.Key);
                foreach (var action in item.Value) action?.Invoke(sprite);
            }
            callback = null;
        }
        public void LoadSprite(string name, Action<Sprite> action)
        {
            if (atlas == null)
            {
                if (callback.TryGetValue(name, out var list))
                {
                    list.Add(action);
                }
                else
                {
                    list = new List<Action<Sprite>> { action };
                    callback.Add(name, list);
                }
            }
            else
            {
                var sprite = GetSprite(name);
                action?.Invoke(sprite);
            }
        }
        private Sprite GetSprite(string name)
        {
            if (sprites.TryGetValue(name, out var sprite))
            {
                return sprite;
            }
            else
            {
                sprite = atlas.GetSprite(name);
                sprites.Add(name, sprite);
                return sprite;
            }
        }
        public void AddRef()
        {
            refCount++;
        }
        public bool RemoveRef()
        {
            refCount--;
            if (refCount <= 0)
            {
                AssetManager.Instance.Unload(ref loadId, CacheTime.Short);
                atlas = null;
                sprites = null;
                callback = null;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
/// <summary>
/// 父节点删除时必须Release，否则加载状态会错乱(虽有办法避免，但不卸载导致的资源常驻无法避免)
/// </summary>
public class LoadGameObjectItem
{
    protected string path;
    protected Object asset;
    private AsyncInstantiateOperation<Object> aio;
    protected GameObject obj;
    private int loadId;
    private LoadState state = LoadState.None;
    private float timer = 0;
    private int timerId = -1;

    private Action<GameObject, object[]> action;
    protected object[] param;

    public void Init(string path, Action<GameObject, object[]> action = null, params object[] param)
    {
        this.path = path;
        this.action = action;
        this.param = param;
    }
    public void Enable()
    {
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
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
            Destroy();
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
        action?.Invoke(obj, param);
    }
    public void Disable()
    {
        obj?.SetActive(false);
        if (timerId < 0) timerId = Driver.Instance.StartTimer(timer, finish: Destroy);
        state |= LoadState.Release;
        action = null;
        param = null;
    }
    public virtual void Destroy()
    {
        asset = null;
        aio = null;
        if (obj != null) GameObject.Destroy(obj);
        obj = null;
        AssetManager.Instance.Unload(ref loadId);
        state = LoadState.None;
        timer = 0;
        Driver.Instance.Remove(timerId);
        timerId = -1;
    }
    private void Recycle()
    {
        state &= LoadState.InstantiateFinish | LoadState.Instantiating | LoadState.LoadFinish | LoadState.Loading;
        timer += GameSetting.recycleTimeS;
        timer = Math.Min(timer, GameSetting.recycleTimeMaxS);
        Driver.Instance.Remove(timerId);
        timerId = -1;
    }
}