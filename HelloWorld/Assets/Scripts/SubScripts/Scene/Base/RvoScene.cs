using UnityEngine;

namespace HotAssembly
{
    public class RvoScene : SceneBase
    {
        private RvoSceneComponent component = new RvoSceneComponent();
        private GameObjectPool<GameObjectPoolItem> pool = new GameObjectPool<GameObjectPoolItem>();

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            SceneManager.Instance.SceneCamera.transform.position = new Vector3(0, 0, -10);
            SceneManager.Instance.SceneCamera.transform.rotation = Quaternion.identity;
            RVOManager.Instance.Init();
            RVOManager.Instance.Start();
            RVOManager.Instance.AddObstacle(component.obstacleObj);
            TimeManager.Instance.StartTimer(30, 0.1f, CreateTest);
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

        private void CreateTest(float t)
        {
            var obj = GameObject.Instantiate(component.tankTransform, Vector3.zero, Quaternion.identity, SceneObj.transform);
            RVOManager.Instance.AddAgent(Vector3.zero, obj.transform, 0.1f);
        }
    }
}