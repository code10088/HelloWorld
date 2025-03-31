using cfg;
using System;
using UnityEngine;

public class SkillCache : EntityCache
{
    public void AddSkill(int skillId, int allyId, Vector3 pos, FightEntity entity)
    {
        var config = ConfigManager.Instance.GameConfigs.TbSkillConfig[skillId];
        Type t;
        int index;
        switch (config.SkillType)
        {
            case SkillType.Damage:
                t = typeof(SkillEntity_Damage);
                index = cache.FindIndex(a => a?.GetType() == t);
                SkillEntity_Damage damage = index < 0 ? new SkillEntity_Damage() : (SkillEntity_Damage)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                damage.Init();
                damage.Init(allyId);
                damage.Init(config, entity);
                entities.Add(damage);
                break;
            case SkillType.Bullet:
                t = typeof(SkillEntity_Bullet);
                index = cache.FindIndex(a => a?.GetType() == t);
                SkillEntity_Bullet bullet = index < 0 ? new SkillEntity_Bullet() : (SkillEntity_Bullet)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                bullet.Init();
                bullet.Init(allyId);
                bullet.Init(config, entity);
                bullet.Init(pos);
                entities.Add(bullet);
                break;
            case SkillType.Entity:
                t = typeof(SkillEntity_Skill);
                index = cache.FindIndex(a => a?.GetType() == t);
                SkillEntity_Skill skill = index < 0 ? new SkillEntity_Skill() : (SkillEntity_Skill)cache[index];
                if (index >= 0) cache.RemoveAt(index);
                skill.Init();
                skill.Init(allyId);
                skill.Init(config, entity);
                entities.Add(skill);
                break;
        }
    }
}