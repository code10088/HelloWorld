using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public partial class UIManager : Singletion<UIManager>, SingletionInterface
{
    public GameObject UIRoot;
    public Transform tUIRoot;
    public Camera UICamera;
    private EventSystem eventSystem;
    private int eventSystemState = 0;
    private Vector2 anchorMin = Vector2.zero;
    public Vector2 anchorMinFull = Vector2.zero;
    public Dictionary<UIWindowType, Transform> Layers;

    private List<UIItem> loadUI = new List<UIItem>();
    private List<UIItem> curUI = new List<UIItem>();
    private List<UIItem> cacheUI = new List<UIItem>();

    public int Layer = 0;

    public void Init()
    {
        UIRoot = GameObject.FindWithTag("UIRoot");
        tUIRoot = UIRoot.transform;
        var temp = GameObject.FindWithTag("UICamera");
        UICamera = temp.GetComponent<Camera>();
        temp = GameObject.FindWithTag("EventSystem");
        eventSystem = temp.GetComponent<EventSystem>();
        //适配
        anchorMin.x = Screen.safeArea.x / Screen.width;
        if (anchorMin.x > 0) anchorMinFull.x = -Screen.safeArea.x / Screen.safeArea.width;
        //层级
        var names = Enum.GetNames(typeof(UIWindowType));
        Layers = new Dictionary<UIWindowType, Transform>();
        for (int i = 0; i < names.Length; i++) Layers[(UIWindowType)i] = tUIRoot.Find(names[i]);
    }
    public void OpenUI(UIType type, Action<bool> open = null, params object[] param)
    {
        UIType from = GetFromUI(type);

        int tempIndex = loadUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            UIItem item = loadUI[tempIndex];
            item.SetParam(from, open, param);
            item.Enable();
            return;
        }

        tempIndex = curUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            UIItem item = curUI[tempIndex];
            item.SetParam(from, open, param);
            curUI.RemoveAt(tempIndex);
            loadUI.Add(item);
            item.Enable();
            return;
        }

        tempIndex = cacheUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            UIItem item = cacheUI[tempIndex];
            item.SetParam(from, open, param);
            cacheUI.RemoveAt(tempIndex);
            loadUI.Add(item);
            item.Enable();
            return;
        }

        UIItem _item = new UIItem(type);
        _item.SetParam(from, open, param);
        loadUI.Add(_item);
        _item.Enable();
    }
    private void InitUI()
    {
        while (loadUI.Count > 0 && loadUI[0].State)
        {
            loadUI[0].Init();
            var t = loadUI[0].Type;
            loadUI.RemoveAt(0);
            EventManager.Instance.FireEvent(EventType.OpenUI, t);
        }
    }
    public void CloseUI(UIType type)
    {
        int tempIndex = loadUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            UIItem item = loadUI[tempIndex];
            item.Disable();
            item.OpenActionInvoke(false);
            loadUI.RemoveAt(tempIndex);
            cacheUI.Add(item);
            return;
        }

        tempIndex = curUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0)
        {
            UIItem item = curUI[tempIndex];
            item.Disable();
            curUI.RemoveAt(tempIndex);
            cacheUI.Add(item);
            EventManager.Instance.FireEvent(EventType.CloseUI, item.Type);
            return;
        }
    }
    public UIBase GetUI(UIType type)
    {
        var result = curUI.Find(a => a.Type == type);
        return result == null ? null : result.BaseUI;
    }
    private UIType GetFromUI(UIType type)
    {
        var result = curUI.FindLast(a => a.Type != type && a.UIWindowType < UIWindowType.Tips);
        return result == null ? UIType.UIMain : result.Type;
    }
    public bool HasOpen(UIType type)
    {
        int tempIndex = loadUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0) return true;
        tempIndex = curUI.FindIndex(a => a.Type == type);
        if (tempIndex >= 0) return true;
        return false;
    }

    public void SetEventSystemState(bool state)
    {
        eventSystemState = state ? Math.Max(eventSystemState - 1, 0) : eventSystemState + 1;
        state = eventSystemState == 0;
        bool cur = eventSystem.enabled;
        if (cur != state) eventSystem.enabled = state;
    }

    private class UIItem
    {
        private UIType from;
        private int loadId;
        private UIConfig config;
        private UIBase baseUI;
        private Object asset;
        private AsyncInstantiateOperation<Object> aio;
        private GameObject baseObj;
        private LoadState state = LoadState.Release;
        private float releaseTime = GameSetting.recycleTimeMinS;
        private int timerId = -1;

        private Action<bool> open = null;
        private object[] param = null;

        public UIType Type => config.UIType;
        public UIWindowType UIWindowType => config.UIWindowType;
        public UIBase BaseUI => baseUI;
        public bool State => state >= LoadState.InstantiateFinish;

        public UIItem(UIType type)
        {
            config = ConfigManager.Instance.TbUIConfig[type];
        }
        public void SetParam(UIType from, Action<bool> open = null, params object[] param)
        {
            this.from = from;
            this.open = open;
            this.param = param;
        }
        public void Enable()
        {
            Instance.SetEventSystemState(false);
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
                    Finish();
                    break;
            }
        }
        private void LoadFinish(int id, Object _asset)
        {
            if (_asset == null)
            {
                Destroy();
                Finish();
            }
            else if (state.HasFlag(LoadState.Release))
            {
                asset = _asset;
                state = LoadState.LoadFinish | LoadState.Release;
                Finish();
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
            }
            else
            {
                baseObj = aio.Result[0] as GameObject;
                bool release = state.HasFlag(LoadState.Release);
                state = LoadState.InstantiateFinish;
                if (release) state |= LoadState.Release;
                baseObj.transform.SetParent(Instance.Layers[config.UIWindowType]);
                baseObj.transform.localPosition = Vector3.zero;
                baseObj.transform.localRotation = Quaternion.identity;
                baseObj.transform.localScale = Vector3.one;
                RectTransform rt = baseObj.GetComponent<RectTransform>();
                rt.anchoredPosition3D = Vector3.zero;
                rt.anchorMin = Instance.anchorMin;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                baseObj.SetActive(false);
            }
            Finish();
        }
        private void Finish()
        {
            Instance.SetEventSystemState(true);
            Instance.InitUI();
        }
        public void Init()
        {
            if (state.HasFlag(LoadState.Release))
            {
                OpenActionInvoke(false);
            }
            else if (baseUI == null)
            {
                Type t = System.Type.GetType(config.Name);
                baseUI = Activator.CreateInstance(t) as UIBase;
                baseUI.Init(baseObj, from, config, param);
                Instance.curUI.Add(this);
                OpenActionInvoke(true);
            }
            else
            {
                baseUI.OnEnable(param);
                Instance.curUI.Add(this);
                OpenActionInvoke(true);
            }
        }
        public void OpenActionInvoke(bool success)
        {
            open?.Invoke(success);
        }
        public void Disable()
        {
            baseUI?.OnDisable();
            if (timerId < 0) timerId = Driver.Instance.StartTimer(releaseTime, finish: Destroy);
            state |= LoadState.Release;
        }
        public void Destroy()
        {
            if (baseUI != null) baseUI.OnDestroy();
            baseUI = null;
            asset = null;
            aio = null;
            if (baseObj != null) GameObject.Destroy(baseObj);
            baseObj = null;
            AssetManager.Instance.Unload(ref loadId);
            state = LoadState.Release;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMinS, 0.2f);
            Driver.Instance.Remove(timerId);
            timerId = -1;
            open = null;
            param = null;
        }
        private void Recycle()
        {
            state &= LoadState.InstantiateFinish | LoadState.Instantiating | LoadState.LoadFinish | LoadState.Loading;
            releaseTime = Mathf.Lerp(releaseTime, GameSetting.recycleTimeMaxS, 0.2f);
            Driver.Instance.Remove(timerId);
            timerId = -1;
        }
    }
}