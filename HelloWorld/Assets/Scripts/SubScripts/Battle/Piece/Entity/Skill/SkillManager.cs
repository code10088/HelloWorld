using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class SkillManager : Singletion<SkillManager>
    {
        private List<SkillEntity> pieces = new List<SkillEntity>(10000);
        private List<SkillEntity> cache = new List<SkillEntity>(10000);

        public void AddSkill(int skillId, int allyId, Vector3 pos, PieceEntity piece)
        {
            var config = ConfigManager.Instance.GameConfigs.TbSkillConfig[skillId];
            Type t;
            int index;
            switch (config.SkillType)
            {
                case SkillType.Damage:
                    t = typeof(SkillEntity_Damage);
                    index = cache.FindIndex(a => a.GetType() == t);
                    SkillEntity_Damage damage = index < 0 ? new SkillEntity_Damage() : (SkillEntity_Damage)cache[index];
                    if (index >= 0) cache.RemoveAt(index);
                    damage.Init();
                    damage.Init(allyId);
                    damage.Init(config, piece);
                    pieces.Add(damage);
                    break;
                case SkillType.Bullet:
                    t = typeof(SkillEntity_Bullet);
                    index = cache.FindIndex(a => a.GetType() == t);
                    SkillEntity_Bullet bullet = index < 0 ? new SkillEntity_Bullet() : (SkillEntity_Bullet)cache[index];
                    if (index >= 0) cache.RemoveAt(index);
                    bullet.Init();
                    bullet.Init(allyId);
                    bullet.Init(config, piece);
                    bullet.Init(pos);
                    pieces.Add(bullet);
                    break;
                case SkillType.Entity:
                    t = typeof(SkillEntity_Skill);
                    index = cache.FindIndex(a => a.GetType() == t);
                    SkillEntity_Skill skill = index < 0 ? new SkillEntity_Skill() : (SkillEntity_Skill)cache[index];
                    if (index >= 0) cache.RemoveAt(index);
                    skill.Init();
                    skill.Init(allyId);
                    skill.Init(config, piece);
                    pieces.Add(skill);
                    break;
            }
        }
        public SkillEntity GetSkill(int id)
        {
            var result = pieces.Find(a => a.ItemId == id);
            return result;
        }
        public T GetSkill<T>(int id) where T : SkillEntity
        {
            var result = GetSkill(id);
            if (result == null) return null;
            else return result as T;
        }
        public void Update()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var temp = pieces[i];
                if (temp.Update(Time.deltaTime))
                {
                    temp.Clear();
                    cache.Add(temp);
                    pieces.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}