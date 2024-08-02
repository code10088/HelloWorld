using UnityEngine;

namespace HotAssembly
{
    public class BattleScene : SceneBase
    {
        private BattleSceneComponent component = new BattleSceneComponent();
        private Camera camera;
        private float dis = 30f;

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);

            camera = SceneManager.Instance.SceneCamera;
            camera.transform.position = new Vector3(0, 0, -dis);
            camera.transform.rotation = Quaternion.identity;
        }
        public override void OnDisable()
        {
            base.OnDisable();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public Transform GetTransform(string path)
        {
            return SceneObj.transform.Find(path);
        }
        public Vector3 ScreenToWorldPoint(Vector2 p)
        {
            return camera.ScreenToWorldPoint(new Vector3(p.x, p.y, dis));
        }
    }
}