using cfg;
using UnityEngine;
using UnityExtensions.Tween;
using System.Collections.Generic;
using System;

public class UIBase
{
    protected GameObject UIObj;
    protected UIType from;
    protected UIConfig config;
    protected Canvas[] layerRecord1;
    protected int[] layerRecord2;
    protected UIParticle[] layerRecord3;
    private List<int> loadId1 = new List<int>();
    private AssetPool loader1 = new AssetPool();
    private AssetObjectPool loader2 = new AssetObjectPool();
    public void Init(GameObject UIObj, UIType from, UIConfig config, params object[] param)
    {
        this.UIObj = UIObj;
        this.from = from;
        this.config = config;
        Init();
        OnEnable(param);
    }
    protected virtual void Init()
    {
        layerRecord1 = UIObj.GetComponentsInChildren<Canvas>(true);
        layerRecord2 = new int[layerRecord1.Length];
        for (int i = 0; i < layerRecord1.Length; i++) layerRecord2[i] = layerRecord1[i].sortingOrder;
        layerRecord3 = UIObj.GetComponentsInChildren<UIParticle>(true);
    }
    public virtual void OnEnable(params object[] param)
    {
        RefreshUILayer();
        PlayEnableAni();
    }
    protected virtual void RefreshUILayer()
    {
        for (int i = 0; i < layerRecord1.Length; i++)
        {
            if (layerRecord1[i] != null)
            {
                UIManager.Instance.Layer += Math.Max(layerRecord2[i], 1);
                layerRecord1[i].sortingLayerName = config.UIWindowType.ToString();
                layerRecord1[i].sortingOrder = UIManager.Instance.Layer;
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
    protected virtual void PlayEnableAni()
    {
        SetTrue();
        TweenPlayer tp = UIObj.GetComponent<TweenPlayer>();
        if (tp) tp.SetForwardDirectionAndEnabled();
    }
    public virtual void OnDisable()
    {
        PlayDisableAni();
    }
    protected virtual void PlayDisableAni()
    {
        TweenPlayer tp = UIObj.GetComponent<TweenPlayer>();
        if (tp)
        {
            tp.SetBackDirectionAndEnabled();
            tp.OnBackArrived = SetFalse;
        }
        else
        {
            SetFalse();
        }
    }
    protected void SetTrue()
    {
        UIObj.SetActive(true);
    }
    protected void SetFalse()
    {
        UIObj.SetActive(false);
    }
    public virtual void OnDestroy()
    {
        layerRecord1 = null;
        layerRecord2 = null;
        layerRecord3 = null;
        loadId1 = null;
        loader1?.Release();
        loader1 = null;
        loader2?.Release();
        loader2 = null;
    }
    protected virtual void OnClose()
    {
        UIManager.Instance.CloseUI(config.UIType);
    }

    #region 扩展方法
    protected void SetSprite(UIImage image, string atlas, string name)
    {
        string path = $"{atlas}{name}.png";
        if (image.loadId > 0)
        {
            loadId1.Remove(image.loadId);
            loader1.Enqueue(path, image.loadId);
        }
        image.loadId = loader1.Dequeue<Sprite>(path, (a, b, c) => image.sprite = (Sprite)b).ItemID;
        loadId1.Add(image.loadId);
    }
    /// <summary>
    /// 背景图
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/UI/Texture的相对路径</param>
    protected void SetSprite(UIRawImage image, string path)
    {
        path = $"{ZResConst.ResUITexturePath}{path}.png";
        if (image.loadId > 0)
        {
            loadId1.Remove(image.loadId);
            loader1.Enqueue(path, image.loadId);
        }
        image.loadId = loader1.Dequeue<Texture>(path, (a, b, c) => image.texture = (Texture)b).ItemID;
        loadId1.Add(image.loadId);
    }
    /// <summary>
    /// 加载Prefab
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/UI/Prefab的相对路径</param>
    protected void LoadPrefab(ref int itemId, string path, Transform parent, Action<GameObject> finish)
    {
        path = $"{ZResConst.ResUIPrefabPath}{path}.prefab";
        if (itemId > 0) loader2.Enqueue(path, itemId);
        itemId = loader2.Dequeue(path, parent, (a, b, c) => finish?.Invoke(b)).ItemID;
    }
    protected void RemovePrefab(ref int itemId, string path)
    {
        if (itemId > 0)
        {
            path = $"{ZResConst.ResUIPrefabPath}{path}.prefab";
            loader2.Enqueue(path, itemId);
            itemId = -1;
        }
    }
    protected void AddEffect(ref int itemId, string path, Transform parent)
    {
        path = $"{ZResConst.ResUIEffectPath}{path}.prefab";
        if (itemId > 0) loader2.Enqueue(path, itemId);
        itemId = loader2.Dequeue(path, parent).ItemID;
    }
    protected void RemoveEffect(ref int itemId, string path)
    {
        if (itemId > 0)
        {
            path = $"{ZResConst.ResUIEffectPath}{path}.prefab";
            loader2.Enqueue(path, itemId);
            itemId = -1;
        }
    }
    protected GameObject Instantiate(GameObject obj, Transform parent = null)
    {
        var result = GameObject.Instantiate(obj);
        if (parent) result.transform.SetParent(parent);
        result.transform.localPosition = Vector3.zero;
        result.transform.localRotation = Quaternion.identity;
        result.transform.localScale = Vector3.one;
        return result;
    }
    #endregion
}