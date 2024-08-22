using System;
using UnityEngine;

namespace HotAssembly
{
    public class GameObjectComponent : ECS_Component
    {
        private int itemId;
        private string path;
        private GameObject obj;
        private Action finish;

        public GameObject Obj => obj;

        public void Init(string path, Transform parent, Action finish)
        {
            if (itemId > 0) Clear();
            if (string.IsNullOrEmpty(path)) return;
            this.path = path;
            this.finish = finish;
            itemId = BattleManager.Instance.Pool.Dequeue(path, parent, LoadFinish).ItemID;
        }
        protected void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            this.obj = obj;
            finish?.Invoke();
        }
        public void Clear()
        {
            if (string.IsNullOrEmpty(path)) return;
            BattleManager.Instance.Pool.Enqueue(path, itemId);
            obj = null;
        }
    }
}