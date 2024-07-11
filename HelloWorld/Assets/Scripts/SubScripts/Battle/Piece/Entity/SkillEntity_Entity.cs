using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class SkillEntity_Entity : SkillEntity
    {
        protected override void PlaySkill()
        {
            for (int i = 0; i < config.Skills.Count; i++)
            {
                pieceSkill.PlaySkill(config.Skills[i]);
            }
        }
    }
}