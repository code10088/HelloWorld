using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class DamageNumManager : Singletion<DamageNumManager>
    {
        private List<DamageNumEntity> pieces = new List<DamageNumEntity>(100);
        private List<DamageNumEntity> cache = new List<DamageNumEntity>(100);

        private GameObjectPool pool = new GameObjectPool();

        public void AddDamageNum(DamageNumType type, string content, Vector3 pos)
        {
            var config = ConfigManager.Instance.GameConfigs.TbDamageNumConfig[type];
            Type t;
            int index;
            switch (config.DamageNumType)
            {
                case DamageNumType.Damage:
                    t = typeof(DamageNumEntity);
                    index = cache.FindIndex(a => a.GetType() == t);
                    DamageNumEntity damage = index < 0 ? new DamageNumEntity() : cache[index];
                    if (index >= 0) cache.RemoveAt(index);
                    damage.Init();
                    damage.Init(config, content, pos);
                    pieces.Add(damage);
                    break;
            }
        }
        public void Remove(DamageNumEntity entity)
        {
            cache.Add(entity);
        }
        public void Clear()
        {
            pieces.Clear();
            cache.Clear();
            pool.Release();
        }
        public int Dequeue(string path, Transform parent, Action<int, GameObject, object[]> action)
        {
            return pool.Dequeue(path, parent, action).ItemID;
        }
        public void Enqueue(string path, int itemId)
        {
            pool.Enqueue(path, itemId);
        }
    }
}