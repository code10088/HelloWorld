using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBase
{
    protected GameObject UIObj;
    protected UIType from;
    protected UIConfig config;
    protected ComponentMark component;
    protected Canvas[] layerRecord1;
    protected int[] layerRecord2;
    protected UIParticle[] layerRecord3;
    private int atlasId = -1;
    private List<int> loadId1;
    private ObjectPoolList loader2;

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
        component = UIObj.GetComponent<ComponentMark>();
        layerRecord1 = UIObj.GetComponentsInChildren<Canvas>(true);
        layerRecord2 = new int[layerRecord1.Length];
        for (int i = 0; i < layerRecord1.Length; i++) layerRecord2[i] = layerRecord1[i].sortingOrder;
        layerRecord3 = UIObj.GetComponentsInChildren<UIParticle>(true);
    }
    public virtual void OnEnable(params object[] param)
    {
        PlayEnableAni();
        RefreshUILayer();
    }
    protected virtual void PlayEnableAni()
    {
        SetTrue();
        var ac = UIObj.GetComponent<AnimationController>();
        if (ac) ac.Play("Open");
    }
    public virtual void OnDisable()
    {
        PlayDisableAni();
    }
    protected virtual void PlayDisableAni()
    {
        var ac = UIObj.GetComponent<AnimationController>();
        if (ac) ac.Play("Close", 0, SetFalse);
        else SetFalse();
    }
    protected void SetTrue()
    {
        UIObj.SetActive(true);
    }
    protected void SetFalse()
    {
        UIObj.SetActive(false);
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
    public virtual void OnDestroy()
    {
        layerRecord1 = null;
        layerRecord2 = null;
        layerRecord3 = null;
        AtlasManager.Instance.UnloadSprite(ref atlasId);
        if (loadId1 != null)
        {
            for (int i = 0; i < loadId1.Count; i++)
            {
                var temp = loadId1[i];
                AssetManager.Instance.Unload(ref temp);
            }
            loadId1 = null;
        }
        if (loader2 != null)
        {
            loader2.Clear();
            loader2 = null;
        }
    }
    protected virtual void OnClose()
    {
        UIManager.Instance.CloseUI(config.UIType);
    }
    protected void OnReture()
    {
        OnClose();
        UIManager.Instance.OpenUI(from);
    }

    #region 扩展方法
    /// <summary>
    /// 图集
    /// </summary>
    protected void SetSprite(Image image, string atlas, string name)
    {
        AtlasManager.Instance.LoadSprite(ref atlasId, atlas, name, sprite => image.sprite = sprite);
    }
    /// <summary>
    /// 非图集
    /// </summary>
    protected void SetSprite(UIImage image, string path, string name)
    {
        if (loadId1 == null) loadId1 = new();
        else loadId1.Remove(image.LoadId);
        image.SetImage($"{path}{name}.png");
        loadId1.Add(image.LoadId);
    }
    /// <summary>
    /// 背景图
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/UI/Texture的相对路径</param>
    protected void SetSprite(UIRawImage image, string path)
    {
        if (loadId1 == null) loadId1 = new();
        else loadId1.Remove(image.LoadId);
        image.SetImage($"{ZResConst.ResUITexturePath}{path}.png");
        loadId1.Add(image.LoadId);
    }
    /// <summary>
    /// 加载Prefab
    /// </summary>
    /// <param name="path">相对于Assets/ZRes/UI/Prefab的相对路径</param>
    protected ObjectPoolListItem LoadPrefab(string path, Action<GameObject, object[]> finish)
    {
        loader2 ??= new();
        path = $"{ZResConst.ResUIPrefabPath}{path}.prefab";
        return loader2.Get(path, finish);
    }
    protected void RemovePrefab(ObjectPoolListItem item)
    {
        loader2.Return(item);
    }
    protected ObjectPoolListItem AddEffect(ref int itemId, string path, Action<GameObject, object[]> finish)
    {
        loader2 ??= new();
        path = $"{ZResConst.ResUIEffectPath}{path}.prefab";
        return loader2.Get(path, finish);
    }
    protected void RemoveEffect(ObjectPoolListItem item)
    {
        loader2.Return(item);
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