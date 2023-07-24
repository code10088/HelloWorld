using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HotAssembly
{
    public class SceneBase
    {
        protected GameObject SceneObj;
        protected int curId;
        protected int fromId;
        protected SceneConfig config;
        public virtual void InitScene(GameObject _SceneObj, int id, int from, SceneConfig _config, params object[] param)
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
        public virtual async UniTask OnEnable(params object[] param)
        {
            await UniTask.Yield();
        }
        public virtual void OnDisable()
        {

        }
        public virtual void OnDestroy()
        {

        }
    }
}