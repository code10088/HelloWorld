using UnityEngine;

namespace HotAssembly
{
    public class SceneBase
    {
        protected GameObject SceneObj;
        protected int curId;
        protected int fromId;
        protected Data_SceneConfig config;
        public virtual void InitScene(GameObject _SceneObj, int id, int from, Data_SceneConfig _config, params object[] param)
        {
            SceneObj = _SceneObj;
            curId = id;
            fromId = from;
            config = _config;
            InitComponent();
            OnEnable(param);
        }
        protected virtual void InitComponent()
        {

        }
        public virtual void OnEnable(params object[] param)
        {

        }
        public virtual void OnDisable()
        {

        }
        public virtual void OnDestroy()
        {

        }
    }
}