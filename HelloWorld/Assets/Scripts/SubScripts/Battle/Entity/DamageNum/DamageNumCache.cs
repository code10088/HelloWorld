using cfg;
using System;
using UnityEngine;

public class DamageNumCache : EntityCache
{
    public void AddDamageNum(DamageNumType type, string content, Vector3 pos)
    {
        var config = ConfigManager.Instance.GameConfigs.TbDamageNumConfig[type];
        Type t;
        int index;
        switch (config.DamageNumType)
        {
            case DamageNumType.Damage:
                t = typeof(DamageNumEntity);
                index = cache.FindIndex(a => a?.GetType() == t);
                DamageNumEntity damage = index < 0 ? new DamageNumEntity() : (DamageNumEntity)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                damage.Init();
                damage.Init(config, content, pos);
                entities.Add(damage);
                break;
        }
    }
}