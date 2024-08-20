using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class BattleScene_Rigidbody : BattleScene
    {
        private BattleScene_RigidbodyComponent component = new BattleScene_RigidbodyComponent();
        private int timerId = -1;
        private int updateId = -1;
        private List<Rigidbody2D> list = new List<Rigidbody2D>();

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);

            timerId = TimeManager.Instance.StartTimer(10, 0.1f, CreateTest, false);
            updateId = Updater.Instance.StartUpdate(Update);
        }
        public override void OnDisable()
        {
            base.OnDisable();

            TimeManager.Instance.StopTimer(timerId);
            Updater.Instance.StopUpdate(updateId);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            list.Clear();
        }

        private void CreateTest(float t)
        {
            var obj = GameObject.Instantiate(component.rigidbodyObj);
            obj.transform.SetParent(SceneObj.transform);
            obj.transform.position = ScreenToWorldPoint(Input.mousePosition);
            var dd = obj.GetComponent<Rigidbody2D>();
            dd.AddForce(Vector2.zero, ForceMode2D.Force);
            list.Add(dd);
        }
        private void Update()
        {
            for (int i = 0; i < list.Count; i++)
            {
                Vector2 a = ScreenToWorldPoint(Input.mousePosition);
                list[i].totalForce = (a - list[i].position).normalized * 0.01f;
            }
        }
    }
}