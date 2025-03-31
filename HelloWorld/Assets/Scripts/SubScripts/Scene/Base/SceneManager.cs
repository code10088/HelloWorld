using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class SceneManager : Singletion<SceneManager>, SingletionInterface
{
    public GameObject SceneRoot;
    public Transform tSceneRoot;
    public Camera SceneCamera;
    private List<SceneItem> loadScene = new List<SceneItem>();
    private List<SceneItem> curScene = new List<SceneItem>();
    private List<SceneItem> cacheScene = new List<SceneItem>();
    private int timerId = -1;

    public void Init()
    {
        SceneRoot = GameObject.FindWithTag("SceneRoot");
        tSceneRoot = SceneRoot.transform;
        var temp = GameObject.FindWithTag("MainCamera");
        SceneCamera = temp.GetComponent<Camera>();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">场景配置表里的id，可重复加载</param>
    /// <param name="open"></param>
    /// <param name="progress"></param>
    /// <param name="param"></param>
    /// <returns>唯一id</returns>
    public int OpenScene(SceneType type, Action<int, bool> open = null, Action<float> progress = null, params object[] param)
    {
        SceneItem item;
        int tempIndex = cacheScene.FindIndex(a => a.Type == type);
        if (tempIndex < 0) item = new SceneItem(type);
        else item = cacheScene[tempIndex];
        SceneType from = SceneType.SceneBase;
        if (curScene.Count > 0) from = curScene[curScene.Count - 1].Type;
        item.SetParam(from, open, progress, param);
        if (tempIndex >= 0) cacheScene.RemoveAt(tempIndex);
        loadScene.Add(item);
        item.Load();
        if (timerId < 0) timerId = TimeManager.Instance.StartTimer(-1, 1, UpdateProgress);
        return item.ID;
    }
    private void InitScene()
    {
        while (loadScene.Count > 0 && loadScene[0].State)
        {
            loadScene[0].Init();
            loadScene.RemoveAt(0);
        }
    }
    public void CloseScene(int id)
    {
        int tempIndex = loadScene.FindLastIndex(a => a.ID == id);
        if (tempIndex >= 0)
        {
            SceneItem item = loadScene[tempIndex];
            item.Release();
            item.OpenActionInvoke(false);
            loadScene.RemoveAt(tempIndex);
            cacheScene.Add(item);
            return;
        }

        tempIndex = curScene.FindLastIndex(a => a.ID == id);
        if (tempIndex >= 0)
        {
            SceneItem item = curScene[tempIndex];
            item.Release();
            curScene.RemoveAt(tempIndex);
            cacheScene.Add(item);
            return;
        }
    }
    public void CloseScene(SceneType type)
    {
        int tempIndex = loadScene.FindLastIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            SceneItem item = loadScene[tempIndex];
            item.Release();
            item.OpenActionInvoke(false);
            loadScene.RemoveAt(tempIndex);
            cacheScene.Add(item);
            return;
        }

        tempIndex = curScene.FindLastIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            SceneItem item = curScene[tempIndex];
            item.Release();
            curScene.RemoveAt(tempIndex);
            cacheScene.Add(item);
            return;
        }
    }
    public SceneBase GetScene(int id)
    {
        var result = curScene.FindLast(a => a.ID == id);
        return result == null ? null : result.BaseScene;
    }
    public SceneBase GetScene(SceneType type)
    {
        var result = curScene.FindLast(a => a.Type == type);
        return result == null ? null : result.BaseScene;
    }
    public bool HasOpen(int id)
    {
        int tempIndex = loadScene.FindLastIndex(a => a.ID == id);
        if (tempIndex >= 0) return true;
        tempIndex = curScene.FindLastIndex(a => a.ID == id);
        if (tempIndex >= 0) return true;
        return false;
    }
    public bool HasOpen(SceneType type)
    {
        int tempIndex = loadScene.FindLastIndex(a => a.Type == type);
        if (tempIndex >= 0) return true;
        tempIndex = curScene.FindLastIndex(a => a.Type == type);
        if (tempIndex >= 0) return true;
        return false;
    }

    public void UpdateProgress(float f)
    {
        if (loadScene.Count > 0) for (int i = 0; i < loadScene.Count; i++) loadScene[i].ProgressActionInvoke();
        else TimeManager.Instance.StopTimer(timerId);
    }

    private class SceneItem
    {
        private static int uniqueId = 0;
        private int id;
        private SceneType from;
        private int loadId;
        private SceneConfig config;
        private SceneBase baseScene;
        private Object asset;
        private AsyncInstantiateOperation<Object> aio;
        private GameObject baseObj;
        private LoadState state = LoadState.Release;
        private float releaseTime = GameSetting.recycleTimeMinS;
        private int timerId = -1;

        private Action<int, bool> open = null;
        private Action<float> progress = null;
        private object[] param = null;

        public int ID => id;
        public SceneType Type => config.SceneType;
        public SceneBase BaseScene => baseScene;
        public bool State => state >= LoadState.InstantiateFinish;

        public SceneItem(SceneType type)
        {
            id = ++uniqueId;
            config = ConfigManager.Instance.GameConfigs.TbSceneConfig[type];
        }
        public void SetParam(SceneType _from, Action<int, bool> _open = null, Action<float> _progress = null, params object[] _param)
        {
            from = _from;
            open = _open;
            param = _param;
            progress = _progress;
            UIManager.Instance.OpenUI(UIType.UISceneLoading);
        }
        public void Load()
        {
            if (state.HasFlag(LoadState.Release))
            {
                Recycle();
            }
            switch (state)
            {
                case LoadState.None:
                    state = LoadState.Loading;
                    AssetManager.Instance.Load<GameObject>(ref loadId, config.PrefabPath, LoadFinish);
                    break;
                case LoadState.Loading:
                    break;
                case LoadState.LoadFinish:
                    LoadFinish(loadId, asset);
                    break;
                case LoadState.Instantiating:
                    break;
                case LoadState.InstantiateFinish:
                    Instance.InitScene();
                    break;
            }
        }
        private void LoadFinish(int id, Object _asset)
        {
            if (_asset == null)
            {
                Release(true);
                Instance.InitScene();
            }
            else if (state.HasFlag(LoadState.Release))
            {
                asset = _asset;
                state = LoadState.LoadFinish | LoadState.Release;
                Instance.InitScene();
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
                Release(true);
            }
            else
            {
                baseObj = aio.Result[0] as GameObject;
                bool release = state.HasFlag(LoadState.Release);
                state = LoadState.InstantiateFinish;
                if (release) state |= LoadState.Release;
                baseObj.transform.SetParent(Instance.tSceneRoot);
                baseObj.transform.localPosition = Vector3.zero;
                baseObj.transform.localRotation = Quaternion.identity;
                baseObj.transform.localScale = Vector3.one;
                baseObj.SetActive(false);
            }
            Instance.InitScene();
        }
        public void Init()
        {
            if (state.HasFlag(LoadState.Release))
            {
                OpenActionInvoke(false);
            }
            else if (baseScene == null)
            {
                baseObj.SetActive(true);
                Type t = System.Type.GetType(config.Name);
                baseScene = Activator.CreateInstance(t) as SceneBase;
                baseScene.InitScene(baseObj, id, from, config, param);
                Instance.curScene.Add(this);
                OpenActionInvoke(true);
            }
            else
            {
                baseObj.SetActive(true);
                baseScene.OnEnable(param);
                Instance.curScene.Add(this);
                OpenActionInvoke(true);
            }
        }
        public void OpenActionInvoke(bool success)
        {
            UIManager.Instance.CloseUI(UIType.UISceneLoading);
            open?.Invoke(id, success);
        }
        public void ProgressActionInvoke()
        {
            float f = AssetManager.Instance.GetProgerss(loadId);
            EventManager.Instance.FireEvent(EventType.SetSceneLoadingProgress, "Loading Scene", f);
            if (progress != null) progress(f);
        }
        public void Release(bool immediate = false)
        {
            baseObj?.SetActive(false);
            baseScene?.OnDisable();
            if (immediate) _Release();
            else if (timerId < 0) timerId = TimeManager.Instance.StartTimer(releaseTime, finish: _Release);
            state |= LoadState.Release;
        }
        private void _Release()
        {
            Instance.cacheScene.Remove(this);
            if (baseScene != null) baseScene?.OnDestroy();
            baseScene = null;
            asset = null;
            aio = null;
            if (baseObj != null) GameObject.Destroy(baseObj);
            baseObj = null;
            AssetManager.Instance.Unload(ref loadId);
            state = LoadState.Release;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMinS, 0.1f);
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
            open = null;
            progress = null;
            param = null;
        }
        private void Recycle()
        {
            state &= LoadState.InstantiateFinish | LoadState.Instantiating | LoadState.LoadFinish | LoadState.Loading;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMaxS, 0.1f);
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
        }
    }
}