using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoSingletion<EffectManager>, SingletionInterface
{
    private static Dictionary<string, AssetObjectPool<EffectItem>> effectDic = new();

    public void Init()
    {
        gameObject.SetActive(false);
    }

    public int Get(string path, Transform parent, Action<GameObject> action = null, float time = 0)
    {
        if (string.IsNullOrEmpty(path)) return -1;
        AssetObjectPool<EffectItem> pool;
        if (!effectDic.TryGetValue(path, out pool))
        {
            pool = new AssetObjectPool<EffectItem>();
            pool.Init(path);
            effectDic.Add(path, pool);
        }
        var temp = pool.Dequeue();
        temp.Init(parent, action, time);
        return temp.ItemID;
    }
    public void Recycle(int id)
    {
        if (id < 0) return;
        foreach (var item in effectDic)
        {
            if (item.Value.Use.Exists(a => a.ItemID == id))
            {
                item.Value.Enqueue(id);
                return;
            }
        }
    }
    public void Clear()
    {
        foreach (var item in effectDic) item.Value.Destroy();
        effectDic.Clear();
    }


    private class EffectItem : ObjectPoolItem
    {
        private Transform parent;
        private Action<GameObject> action;
        private int timerId = -1;

        public void Init(Transform parent, Action<GameObject> action = null, float time = 0)
        {
            this.parent = parent;
            this.action = action;
            if (time > 0) timerId = Driver.Instance.StartTimer(time, finish: Recycle);
        }
        protected override void Finish(GameObject obj)
        {
            base.Finish(obj);
            if (obj == null) return;
            obj.transform.parent = parent;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            action?.Invoke(obj);
        }
        public override void Disable()
        {
            base.Disable();
            if (obj) obj.transform.parent = Instance.transform;
            Driver.Instance.Remove(timerId);
            timerId = -1;
        }
        private void Recycle()
        {
            Instance.Recycle(ItemID);
        }
    }
}