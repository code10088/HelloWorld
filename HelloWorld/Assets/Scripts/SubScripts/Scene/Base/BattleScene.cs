using UnityEngine;

namespace HotAssembly
{
    public class BattleScene : SceneBase
    {
        private BattleSceneComponent component = new BattleSceneComponent();
        private GameObjectPool<GameObjectPoolItem> pool = new GameObjectPool<GameObjectPoolItem>();

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
            pool.Init($"{ZResConst.ResUIPrefabPath}TestBullet.prefab");
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);

        }
        public override void OnDisable()
        {
            base.OnDisable();

        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            pool.Release();
        }

        public Transform GetTransform(string path)
        {
            return SceneObj.transform.Find(path);
        }
    }
}