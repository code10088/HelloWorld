using cfg;
using System.Collections.Generic;
using UnityEngine;

public class SceneBase
{
    protected GameObject SceneObj;
    protected int id;
    protected SceneType from;
    protected SceneConfig config;
    private List<int> loadId1;
    private AssetObjectPool loader2;

    public virtual void InitScene(GameObject _SceneObj, int _id, SceneType _from, SceneConfig _config, params object[] param)
    {
        SceneObj = _SceneObj;
        id = _id;
        from = _from;
        config = _config;
        Init();
        OnEnable(param);
    }
    protected virtual void Init()
    {

    }
    public virtual void OnEnable(params object[] param)
    {
        SceneManager.Instance.CameraController.SetTRS(config.CameraPos, 0, config.CameraEuler);

        if(config.SkyBoxPath.Length > 0)
        {
            if (loadId1 == null) loadId1 = new();
            int skyboxLoadId = -1;
            AssetManager.Instance.Load<Material>(ref skyboxLoadId, config.SkyBoxPath, (a, b) => RenderSettings.skybox = (Material)b);
            loadId1.Add(skyboxLoadId);
        }
    }
    public virtual void OnDisable()
    {

    }
    public virtual void OnDestroy()
    {
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
            loader2.Destroy();
            loader2 = null;
        }
    }
    protected void OnClose()
    {
        SceneManager.Instance.CloseScene(id);
    }
    protected void OnReture()
    {
        OnClose();
        SceneManager.Instance.OpenScene(from);
    }
}