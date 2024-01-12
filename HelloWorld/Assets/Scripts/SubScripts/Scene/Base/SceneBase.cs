using cfg;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class SceneBase
    {
        protected GameObject SceneObj;
        protected int id;
        protected SceneType from;
        protected SceneConfig config;
        private List<int> loader1 = new List<int>();
        private List<LoadGameObjectItem> loader2 = new List<LoadGameObjectItem>();

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
            int skyboxLoadId = -1;
            AssetManager.Instance.Load<Material>(ref skyboxLoadId, config.SkyBoxPath, (a, b) => RenderSettings.skybox = (Material)b);
            loader1.Add(skyboxLoadId);
        }
        public virtual void OnDisable()
        {
            for (int i = 0; i < loader1.Count; i++) AssetManager.Instance.Unload(loader1[i]);
            for (int i = 0; i < loader2.Count; i++) loader2[i].SetActive(false);
            loader1.Clear();
        }
        public virtual void OnDestroy()
        {
            for (int i = 0; i < loader2.Count; i++) loader2[i].Release();
            loader1 = null;
            loader2 = null;
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
}