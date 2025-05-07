using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : Singletion<EffectManager>
{
    private static Dictionary<string, AssetObjectPool<ObjectPoolItem>> effectDic = new();
    private List<EffectItem> list = new();
    private Queue<EffectItem> cache = new();

    public int AddEffect(string path, Transform parent = null, Action<GameObject> action = null, float time = -1)
    {
        if (string.IsNullOrEmpty(path)) return -1;
        EffectItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init(path, parent, action, time);
        list.Add(temp);
        return temp.ItemID;
    }
    public void Remove(int id)
    {
        if (id < 0) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].ItemID == id)
            {
                list[i].Reset();
                cache.Enqueue(list[i]);
                list.RemoveAt(i);
                return;
            }
        }
    }
    public void Clear()
    {
        foreach (var item in list) item.Reset();
        foreach (var item in effectDic) item.Value.Release();
        list.Clear();
        effectDic.Clear();
    }


    private class EffectItem : AsyncItem
    {
        private string path;
        private Transform parent;
        private Action<GameObject> action;
        private int poolItemId;
        private int timerId = -1;

        public void Init(string path, Transform parent = null, Action<GameObject> action = null, float time = 0)
        {
            base.Init(finish);
            this.path = path;
            this.parent = parent;
            this.action = action;
            AssetObjectPool<ObjectPoolItem> pool;
            if (!effectDic.TryGetValue(path, out pool))
            {
                pool = new AssetObjectPool<ObjectPoolItem>();
                pool.Init(path);
                effectDic.Add(path, pool);
            }
            poolItemId = pool.Dequeue(LoadFinish).ItemID;
            if (time > 0) timerId = TimeManager.Instance.StartTimer(time, finish: Remove);
        }
        private void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            if (parent != null)
            {
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
            }
            if (action != null)
            {
                action.Invoke(obj);
            }
        }
        private void Remove()
        {
            Instance.Remove(ItemID);
        }
        public override void Reset()
        {
            base.Reset();
            if (effectDic.TryGetValue(path, out AssetObjectPool<ObjectPoolItem> pool)) pool.Enqueue(poolItemId);
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
        }
    }
}