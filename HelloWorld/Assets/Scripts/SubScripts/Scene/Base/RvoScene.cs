using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using Random = System.Random;

namespace HotAssembly
{
    public class RvoScene : SceneBase
    {
        private RvoSceneComponent component = new RvoSceneComponent();
        private Camera camera;
        private float dis = 30f;
        private int updateId = -1;
        private Random random = new Random();
        private List<RvoItem> items = new List<RvoItem>();
        private Queue<RvoItem> cache = new Queue<RvoItem>();
        private int timerId = -1;
        private Vector3 targetPos;

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);

            RVOManager.Instance.Init();
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);

            camera = SceneManager.Instance.SceneCamera;
            camera.transform.position = new Vector3(0, 0, -dis);
            camera.transform.rotation = Quaternion.identity;

            updateId = Updater.Instance.StartUpdate(Update);
            RVOManager.Instance.AddObstacle(component.obstacleObj);
            RVOManager.Instance.Start();
            timerId = TimeManager.Instance.StartTimer(30, 0.1f, CreateTest);
        }
        public override void OnDisable()
        {
            base.OnDisable();

            Updater.Instance.StopUpdate(updateId);
            RVOManager.Instance.Stop();
            TimeManager.Instance.StopTimer(timerId);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            RVOManager.Instance.Clear();
            RvoItem.Release();
            items.Clear();
            cache.Clear();
        }

        private void CreateTest(float t)
        {
            var angle = random.NextDouble() * 2.0f * Math.PI;
            var pos = component.startTransform.position;
            pos.x += (float)Math.Cos(angle);
            pos.y += (float)Math.Sin(angle);
            RvoItem item = cache.Count > 0 ? cache.Dequeue() : new RvoItem();
            item.Init(ZResConst.ResScenePath + "RvoSphere", SceneObj.transform, pos);
            item.SetAgentRadius(0.5f);
            item.SetAgentMaxSpeed(1f);
            items.Add(item);
        }
        private void Update()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts != null)
            {
                TouchControl tc = ts.touches[0];
                var p = tc.position.ReadValue();
                targetPos = camera.ScreenToWorldPoint(new Vector3(p.x, p.y, dis));
            }
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.RefreshTarget(targetPos);
                var dis = Vector3.Distance(item.Pos, component.endTransform.position);
                if (dis > 1) continue;
                item.Clear();
                cache.Enqueue(item);
                items.RemoveAt(i);
                i--;
            }
        }
    }
    public class RvoItem
    {
        private static GameObjectPool pool = new GameObjectPool();
        public static void Release() => pool.Release();

        private int itemId = -1;
        private string path;
        private GameObject obj;
        private Vector3 pos;
        private int agentId = -1;
        private Vector3 target;
        private float radius;
        private float speed;

        public Vector3 Pos => pos;
        private bool Active => obj;

        public void Init(string path, Transform parent, Vector3 pos)
        {
            if (string.IsNullOrEmpty(path)) return;
            this.path = path;
            this.pos = pos;
            itemId = pool.Dequeue(path, parent, LoadFinish).ItemID;
        }
        private void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            this.obj = obj;
            obj.transform.position = pos;
            agentId = RVOManager.Instance.AddAgent(pos, Change);
            RefreshTarget(target);
            SetAgentRadius(radius);
            SetAgentMaxSpeed(speed);
        }
        private void Change(Vector3 pos)
        {
            if (!Active) return;
            this.pos = pos;
            obj.transform.position = pos;
            var dir = target - pos;
            float angle = Vector2.SignedAngle(Vector2.right, dir);
            obj.transform.eulerAngles = new Vector3(0, 0, angle);
        }
        public void Clear()
        {
            RVOManager.Instance.RemoveAgent(agentId);
            agentId = -1;
            pool.Enqueue(path, itemId);
            itemId = -1;
            obj = null;
        }
        public void RefreshTarget(Vector3 target)
        {
            this.target = target;
            if (!Active) return;
            RVOManager.Instance.RefreshTarget(agentId, target);
        }
        public void SetAgentRadius(float radius)
        {
            this.radius = radius;
            if (!Active) return;
            RVOManager.Instance.SetAgentRadius(agentId, radius);
        }
        public void SetAgentMaxSpeed(float speed)
        {
            this.speed = speed;
            if (!Active) return;
            RVOManager.Instance.SetAgentMaxSpeed(agentId, speed);
        }
    }
}