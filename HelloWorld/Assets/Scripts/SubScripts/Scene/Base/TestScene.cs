using UnityEngine;

namespace HotAssembly
{
    public class TestScene : SceneBase
    {
        private TestSceneComponent component = new TestSceneComponent();
        private GameObjectPool<GameObjectPoolItem> pool = new GameObjectPool<GameObjectPoolItem>();

        private int testEffectId = -1;

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
            pool.Init($"{ZResConst.ResUIPrefabPath}TestBullet.prefab");
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            GameDebug.Log("TestScene OnEnable");

            Camera camera = SceneManager.Instance.SceneCamera;
            camera.transform.position = new Vector3(0, 3, -3);
            camera.transform.eulerAngles = new Vector3(30, 0, 0);
            testEffectId = EffectManager.Instance.AddEffect($"{ZResConst.ResSceneEffectPath}Fire.prefab", component.fireRootTransform);
        }
        public override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("TestScene OnDisable");

            EffectManager.Instance.Remove(testEffectId);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            GameDebug.Log("TestScene OnDestroy");
            pool.Release();
        }

        public void LoadBulletFromPool()
        {
            pool.Dequeue(component.obj.transform, (a, b, c) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
            pool.Dequeue(component.obj.transform, (a, b, c) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
            pool.Enqueue(pool.Use[0].ItemID);
            pool.Dequeue(component.obj.transform, (a, b, c) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
        }
        public void DelectBullet()
        {
            pool.Enqueue(pool.Use[0].ItemID);
        }
    }
}