using UnityEngine;

namespace HotAssembly
{
    public class SkillEntity_Damage : SkillEntity
    {
        private int count = 0;

        protected override void PlaySkill()
        {
            if (target == null) return;
            //…À∫¶¥Œ ˝
            int count1 = 1;
            if (config.Internal > 0)
            {
                int count2 = 1 + Mathf.FloorToInt((timer - config.Delay) / config.Internal);
                count1 = count2 - count;
                count = count2;
            }
            if (count1 == 0) return;
            //…À∫¶√¸÷–
            for (int i = 0; i < count1; i++)
            {
                BattleCalculation.Instance.Attack(this, target);
            }
        }
    }
}