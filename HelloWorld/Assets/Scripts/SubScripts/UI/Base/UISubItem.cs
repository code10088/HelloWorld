using UnityEngine;
using System;
using Object = UnityEngine.Object;

public class UISubItem : UIBase
{
    #region 加载
    private string prefabPath;
    private Transform parent;
    private int loadId;
    private Object asset;
    private AsyncInstantiateOperation<Object> aio;
    private LoadState state = LoadState.Release;
    private float releaseTime = 10f;
    private int timerId = -1;

    private Action<bool> open = null;
    private object[] param = null;

    /// <summary>
    /// 子界面
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/UI/Prefab的相对路径</param>
    public UISubItem(string path)
    {
        prefabPath = path;
    }
    private void Enable(Transform parent, Action<bool> open = null, params object[] param)
    {
        this.parent = parent;
        this.open = open;
        this.param = param;
        if (state.HasFlag(LoadState.Release))
        {
            Recycle();
        }
        switch (state)
        {
            case LoadState.None:
                state = LoadState.Loading;
                AssetManager.Instance.Load<GameObject>(ref loadId, $"{ZResConst.ResUIPrefabPath}{prefabPath}.prefab", LoadFinish);
                break;
            case LoadState.Loading:
                break;
            case LoadState.LoadFinish:
                LoadFinish(loadId, asset);
                break;
            case LoadState.Instantiating:
                break;
            case LoadState.InstantiateFinish:
                OnEnable(param);
                open?.Invoke(true);
                break;
        }
    }
    private void LoadFinish(int id, Object _asset)
    {
        if (_asset == null)
        {
            Destroy();
            open?.Invoke(false);
        }
        else if (state.HasFlag(LoadState.Release))
        {
            asset = _asset;
            state = LoadState.LoadFinish | LoadState.Release;
            open?.Invoke(false);
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
            open?.Invoke(false);
        }
        else if (state.HasFlag(LoadState.Release))
        {
            UIObj = aio.Result[0] as GameObject;
            state = LoadState.InstantiateFinish | LoadState.Release;
            UIObj.transform.SetParent(parent);
            UIObj.transform.localPosition = Vector3.zero;
            UIObj.transform.localRotation = Quaternion.identity;
            UIObj.transform.localScale = Vector3.one;
            RectTransform rt = UIObj.GetComponent<RectTransform>();
            rt.anchoredPosition3D = Vector3.zero;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Init();
            SetFalse();
            open?.Invoke(false);
        }
        else
        {
            UIObj = aio.Result[0] as GameObject;
            state = LoadState.InstantiateFinish;
            UIObj.transform.SetParent(parent);
            UIObj.transform.localPosition = Vector3.zero;
            UIObj.transform.localRotation = Quaternion.identity;
            UIObj.transform.localScale = Vector3.one;
            RectTransform rt = UIObj.GetComponent<RectTransform>();
            rt.anchoredPosition3D = Vector3.zero;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Init();
            OnEnable(param);
            open?.Invoke(true);
        }
    }
    private void Disable()
    {
        if (state.HasFlag(LoadState.InstantiateFinish)) OnDisable();
        if (timerId < 0) timerId = Driver.Instance.StartTimer(releaseTime, finish: Destroy);
        state |= LoadState.Release;
    }
    private void Destroy()
    {
        OnDestroy();
        parent = null;
        asset = null;
        aio = null;
        if (UIObj != null) GameObject.Destroy(UIObj);
        UIObj = null;
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
    #endregion

    private bool active = false;
    public bool Active => active;

    public void Open(Transform parent, Action<bool> open = null, params object[] param)
    {
        active = true;
        Enable(parent, open, param);
    }
    public void Close(bool immediate = false)
    {
        active = false;
        if (immediate) Destroy();
        else Disable();
    }
    protected override void RefreshUILayer()
    {
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        for (int i = 0; i < layerRecord1.Length; i++)
        {
            if (layerRecord1[i] != null)
            {
                layerRecord1[i].sortingLayerName = canvas.sortingLayerName;
                layerRecord1[i].sortingOrder = canvas.sortingOrder + layerRecord2[i];
            }
        }
        for (int i = 0; i < layerRecord3.Length; i++)
        {
            if (layerRecord3[i] != null)
            {
                layerRecord3[i].Refresh();
            }
        }
    }
}