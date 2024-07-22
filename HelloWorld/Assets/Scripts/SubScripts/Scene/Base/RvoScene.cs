using System;
using UnityEngine;
using Random = System.Random;

namespace HotAssembly
{
    public class RvoScene : SceneBase
    {
        private RvoSceneComponent component = new RvoSceneComponent();
        private Camera camera;
        private int updateId = -1;
        private float dis = 30f;
        private Random random = new Random();
        private Vector3 target = new Vector3(-29, -16);

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
            if (updateId < 0) updateId = Updater.Instance.StartUpdate(Update);
            RVOManager.Instance.Init();
            RVOManager.Instance.AddObstacle(component.obstacleObj);
            TimeManager.Instance.StartTimer(50, 0.1f, CreateTest);
            RVOManager.Instance.Start();
        }
        public override void OnDisable()
        {
            base.OnDisable();

            if (updateId > 0) Updater.Instance.StopUpdate(updateId);
            RVOManager.Instance.Stop();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            RVOManager.Instance.Clear();
        }

        private void CreateTest(float t)
        {
            var angle = random.NextDouble() * 2.0f * Math.PI;
            var pos = component.tankTransform.position;
            pos.x += (float)Math.Cos(angle);
            pos.y += (float)Math.Sin(angle);
            var obj = GameObject.Instantiate(component.tankTransform, SceneObj.transform);
            RVOManager.Instance.AddAgent(pos, obj, 0.5f);
        }
        private void Update()
        {
            Vector3 v = camera.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * dis);
            var agents = RVOManager.Instance.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                agents[i].RefreshTarget(v);
                var dis = Vector3.Distance(agents[i].Transform.position, target);
                if (dis < 2) RVOManager.Instance.RemoveAgent(agents[i].AgentId);
            }
        }
    }
}