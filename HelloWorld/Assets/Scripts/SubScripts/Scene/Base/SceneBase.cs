using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class SceneBase
    {
        protected GameObject SceneObj;
        protected int id;
        protected SceneType from;
        protected SceneConfig config;
        private int skyboxLoadId = -1;

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
            AssetManager.Instance.Load<Material>(ref skyboxLoadId, config.SkyBoxPath, (a, b) => RenderSettings.skybox = (Material)b);
        }
        public virtual void OnEnable(params object[] param)
        {

        }
        public virtual void OnDisable()
        {

        }
        public virtual void OnDestroy()
        {
            AssetManager.Instance.Unload(skyboxLoadId);
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